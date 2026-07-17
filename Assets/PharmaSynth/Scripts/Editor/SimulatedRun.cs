#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Plays a module END-TO-END in edit mode, driving the REAL wiring the player
/// would (user 2026-07-17: "I want you to see the bugs yourselves when you play
/// the game before I test it manually"). Not a replacement for the headset pass —
/// it can't feel a grab or see a misplaced visual — but it catches the
/// dead-on-arrival class before a human wastes VR time on it:
///   • a step no mechanism can complete (missing binding/station/rack wiring)
///   • thresholds that complete early or never (the accumulation contract)
///   • rack groups that never fire after every member is served
///   • a required chemical with NO source on the bench, or too little of it
///   • pours that land invisibly small in their vessel
///   • correct play being flagged as a mistake (mis-authored bindings)
///
/// HOW it simulates — the PLAYER PATH, not the plumbing (user 2026-07-17: "do
/// not cheat by programmatically connecting things; you wouldn't see issues"):
/// builder.Build() wires the real scene, runner.StartExperiment() opens the real
/// graph, and every reagent is then TRANSFERRED the way a hand would — drawn out
/// of the actual bench source bottle (PourOut) and landed through
/// LiquidPhysics.AddLiquid in VERB-CONTRACT increments (1 ml a squeeze, 0.1 g a
/// spatula dip, 0.5 ml tilt-pour ticks WITH human overshoot). Completion may
/// only arrive through the real event chain: AddLiquid → LiquidAdded → binding →
/// CompleteTask, rack-group polls, ZoneSimStation sims. The first version drove
/// binding.HandleReagent directly and reported Exp 2 CLEAN while a real player
/// was hard-stuck — the binding had never subscribed to its vessel's events, a
/// bug the direct call bypassed entirely. Never again: pours go through the
/// bottle or they don't count.
public static class SimulatedRun
{
    public class Result
    {
        public int totalTasks, completedTasks, mistakes;
        public readonly List<string> bugs = new List<string>();      // would block or corrupt a run
        public readonly List<string> warnings = new List<string>();  // playable but wrong-feeling
        public bool Clean => bugs.Count == 0 && completedTasks == totalTasks && mistakes == 0;
    }

    [MenuItem("Tools/PharmaSynth/Simulate Run/Prelim 1 — Chemical Compounding")]
    public static void SimCompounding() => Menu("prelim-chemical-compounding");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Prelim 2 — Ethyl Alcohol")]
    public static void SimEthyl() => Menu("prelim-ethyl-alcohol");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Midterm 1 — Benzoic Acid")]
    public static void SimBenzoic() => Menu("midterm-benzoic-acid");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Midterm 2 — Acetanilide")]
    public static void SimAcetanilide() => Menu("midterm-acetanilide");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Midterm 3 — Acetone")]
    public static void SimAcetone() => Menu("midterm-acetone");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Midterm 4 — Chloroform")]
    public static void SimChloroform() => Menu("midterm-chloroform");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Final 1 — Benzamide")]
    public static void SimBenzamide() => Menu("final-benzamide");
    [MenuItem("Tools/PharmaSynth/Simulate Run/Final 2 — Wine Making")]
    public static void SimWine() => Menu("final-winemaking");

    static void Menu(string moduleId)
    {
        if (Application.isPlaying) { Debug.LogWarning("[SimRun] exit Play mode first."); return; }
        var log = new StringBuilder();
        var r = Run(moduleId, log);
        string path = "Logs/simrun-" + moduleId + ".txt";
        System.IO.File.WriteAllText(path, log.ToString());
        string verdict = r == null ? "COULD NOT RUN"
            : r.Clean ? "CLEAN — " + r.completedTasks + "/" + r.totalTasks + " tasks, 0 mistakes"
            : r.bugs.Count + " BUG(S), " + r.completedTasks + "/" + r.totalTasks + " tasks, "
              + r.mistakes + " mistakes, " + r.warnings.Count + " warning(s)";
        Debug.Log((r != null && r.Clean ? "<color=#4CD07D>" : "<color=#FF7A6B>")
                  + "[SimRun] " + moduleId + ": " + verdict + "</color>\n  full transcript → " + path);
    }

    /// Full simulated playthrough. Also callable from the suite (regression lock).
    public static Result Run(string moduleId, StringBuilder log)
    {
        var builder = Object.FindAnyObjectByType<ExperimentSceneBuilder>();
        var runner = Object.FindAnyObjectByType<ExperimentRunner>();
        var lib = AssetDatabase.LoadAssetAtPath<ExperimentLibrary>(
            "Assets/PharmaSynth/ScriptableObjects/ExperimentLibrary.asset");
        var module = lib != null ? lib.Get(moduleId) : null;
        if (builder == null || runner == null || module == null)
        {
            log.AppendLine("missing " + (builder == null ? "builder " : "")
                           + (runner == null ? "runner " : "") + (module == null ? "module" : ""));
            return null;
        }

        // The player path DRAINS real bottles and FILLS real tubes — snapshot
        // every vessel's contents first and restore after, or each edit-mode sim
        // permanently corrupts the saved scene's supplies.
        var snapshot = new List<(LiquidPhysics lp, ChemicalData chem, float ml, ChemicalData ppt, float pptMl)>();
        foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            snapshot.Add((lp, lp.currentChemical, lp.currentLiquidVolume, lp.currentPptChemical, lp.currentPptVolume));

        // Real wiring, real graph — exactly what a campaign start does.
        builder.Build(moduleId);
        runner.SetModule(module);
        runner.StartExperiment();

        var res = new Result { totalTasks = module.graphTasks.Count };
        log.AppendLine("=== Simulated run: " + moduleId + " (" + res.totalTasks + " tasks) ===");

        // Every mistake as it lands, in context — a count alone can't tell a
        // mis-authored binding from a fume-hood rule firing on a bench pour.
        System.Action<LabErrorType, string> onMistake =
            (type, msg) => log.AppendLine("  ⚠ MISTAKE [" + type + "] " + msg);
        runner.Mistakes.MistakeRecorded += onMistake;

        // Index the mechanisms the build produced.
        var bindings = Object.FindObjectsByType<LiquidTaskBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var racks = Object.FindObjectsByType<RackTaskGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var simStations = Object.FindObjectsByType<ZoneSimStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var zoneStations = Object.FindObjectsByType<ExperimentTaskStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var temps = Object.FindObjectsByType<TemperatureSim>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var labItems = Object.FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var stepsByTask = new Dictionary<string, List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)>>();
        foreach (var b in bindings)
            foreach (var s in b.ExpectedSteps)
            {
                if (s == null || string.IsNullOrEmpty(s.taskId)) continue;
                if (!stepsByTask.TryGetValue(s.taskId, out var l)) stepsByTask[s.taskId] = l = new List<(LiquidTaskBinding, LiquidTaskBinding.ReagentStep)>();
                l.Add((b, s));
            }

        var demand = new Dictionary<ChemicalData, float>();          // supply audit
        var pouredInto = new Dictionary<LiquidTaskBinding, float>(); // fill-visibility audit
        var done = new HashSet<string>();

        // Walk until finished or stuck. Each pass serves every currently-available task.
        for (int pass = 0; pass < res.totalTasks + 2 && done.Count < res.totalTasks; pass++)
        {
            bool progressed = false;
            foreach (var t in new List<ExperimentTask>(runner.Graph.AvailableTasks()))
            {
                string id = t.taskId;
                if (done.Contains(id)) continue;
                log.AppendLine("\n» " + id + " — \"" + t.label + "\"");

                bool handled;
                if (stepsByTask.TryGetValue(id, out var steps))
                {
                    handled = true;
                    if (SimulatePours(runner, id, steps, demand, pouredInto, res, log)
                        && !runner.Graph.IsComplete(id))
                    {
                        // Delivery done but completion belongs elsewhere: a rack
                        // group (comparison sets) or a station (pour THEN heat —
                        // Exp 2's hydrolysis boil is exactly this handoff).
                        bool deferred = false;
                        foreach (var (_, s) in steps) if (!s.completesTask) deferred = true;
                        RackTaskGroup rack = null;
                        foreach (var r in racks) if (r != null && r.TaskId == id) rack = r;
                        if (rack != null)
                        {
                            if (!rack.ShouldFire())
                            {
                                res.bugs.Add(id + ": rack group refuses to fire after every member was served ("
                                             + rack.MemberCount + " members)");
                                log.AppendLine("  ✗ rack not ready after full service");
                            }
                            else
                            {
                                runner.CompleteTask(id);   // exactly what the group's Update does in play
                                log.AppendLine("  ✓ rack fired (" + rack.MemberCount + " tubes)");
                            }
                        }
                        else if (SimulateStation(runner, id, simStations, zoneStations, temps, labItems, res, log)) { }
                        else if (deferred)
                        {
                            res.bugs.Add(id + ": steps defer completion (completesTask:false) but NO rack group or station owns the task");
                            log.AppendLine("  ✗ deferred but ownerless");
                        }
                        else
                            res.bugs.Add(id + ": every step delivered its full amount yet the task did not complete");
                    }
                    else if (runner.Graph.IsComplete(id))
                        log.AppendLine("  ✓ completed by delivery");
                }
                else
                    handled = SimulateStation(runner, id, simStations, zoneStations, temps, labItems, res, log);

                if (!handled)
                {
                    // No binding, no station: maybe an externally-registered condition.
                    runner.Graph.Tick();
                    if (!runner.Graph.IsComplete(id))
                    {
                        res.bugs.Add(id + ": NO mechanism completes this task (no binding, station, or satisfied condition)");
                        log.AppendLine("  ✗ no mechanism — the player could never finish this step");
                        done.Add(id);           // skip so the walk can report what else breaks
                        runner.CompleteTask(id); // force past it to keep exploring downstream
                    }
                }

                if (runner.Graph.IsComplete(id)) { done.Add(id); res.completedTasks++; progressed = true; }
            }
            if (!progressed && done.Count < res.totalTasks)
            {
                var avail = new List<string>();
                foreach (var t in runner.Graph.AvailableTasks()) avail.Add(t.taskId);
                res.bugs.Add("DEADLOCK after " + done.Count + "/" + res.totalTasks
                             + " tasks; available-but-stuck: " + string.Join(", ", avail));
                break;
            }
        }

        AuditSupplies(demand, labItems, res, log);
        AuditVisibility(pouredInto, res, log);

        res.mistakes = runner.MistakeCount;
        if (res.mistakes > 0)
            res.bugs.Add("a PERFECT run logged " + res.mistakes + " mistake(s) — correct play is being graded as wrong");

        log.AppendLine("\n=== verdict: " + res.completedTasks + "/" + res.totalTasks + " tasks · "
                       + res.mistakes + " mistakes · " + res.bugs.Count + " bugs · " + res.warnings.Count + " warnings ===");
        foreach (var b in res.bugs) log.AppendLine("  BUG  " + b);
        foreach (var w in res.warnings) log.AppendLine("  WARN " + w);

        // TEAR DOWN, don't leave a revealed stage: Build("tutorial-methane") is a
        // pure clear (stage children incl. TeleAnchor_* pads, DynLabel_* billboards,
        // bench bindings) that spawns nothing — methane's stage is hand-built. The
        // sim used to end on Build(moduleId), which left the guidance litter in the
        // scene after every suite run; the user kept hand-deleting DynLabels and
        // TeleAnchors that the next run resurrected (2026-07-17). Use Reveal Stage
        // when you WANT the built stage to inspect.
        runner.Mistakes.MistakeRecorded -= onMistake;
        builder.Build("tutorial-methane");

        // Put every drop back: bottles refill, tubes empty — the scene must be
        // byte-identical to before the run.
        foreach (var (lp, chem, ml, ppt, pptMl) in snapshot)
        {
            if (lp == null) continue;
            lp.SetContents(chem, ml);
            lp.currentPptChemical = ppt;
            lp.currentPptVolume = pptMl;
        }
        return res;
    }

    /// Deliver every reagent step of one task THE WAY A HAND WOULD: draw each
    /// increment out of the real bench source bottle and land it through
    /// LiquidPhysics.AddLiquid — the full event chain (wake/mix/hazard/ledger/
    /// binding) runs for every action. Bulk tilt-pours overshoot ~20% like a
    /// human, which also proves the leniency contract (thresholds are minimums).
    /// Returns false when the task completed EARLY (already reported as a bug).
    static bool SimulatePours(ExperimentRunner runner, string id,
        List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)> steps,
        Dictionary<ChemicalData, float> demand,
        Dictionary<LiquidTaskBinding, float> pouredInto, Result res, StringBuilder log)
    {
        int totalActions = 0, doneActions = 0;
        foreach (var (b, s) in steps) totalActions += ActionsFor(s);

        foreach (var (b, s) in steps)
        {
            var lp = b.GetComponent<LiquidPhysics>();
            if (lp == null)
            {
                res.bugs.Add(id + ": binding for " + s.reagent.chemicalName + " sits on '" + b.name
                             + "' which has NO LiquidPhysics — nothing to pour into");
                return false;
            }
            var src = FindSource(s.reagent);
            if (src == null)
            {
                res.bugs.Add(id + ": no bench source holds '" + s.reagent.chemicalName
                             + "' — the player cannot perform this step");
                log.AppendLine("  ✗ no source bottle for " + s.reagent.chemicalName);
                continue;   // supply audit details it; keep walking
            }

            float inc = IncrementFor(s);
            int n = ActionsFor(s);
            // Human overshoot on bulk tilt-pours only — squeezes and dips are counted.
            if (!IsSolid(s) && s.requiredMl > 5f) n = Mathf.CeilToInt(n * 1.2f);

            float before = b.AccumulatedFor(id, s.reagent);
            for (int k = 0; k < n; k++)
            {
                var chem = src.PourOut(inc);                    // out of the real bottle
                if (chem == null)
                {
                    res.warnings.Add(id + ": source " + src.name + " ran DRY mid-step ("
                                     + s.reagent.chemicalName + ") — starvation");
                    break;
                }
                lp.AddLiquid(chem, inc);                        // the real chemistry path
                doneActions++;
                if (runner.Graph.IsComplete(id) && doneActions < totalActions)
                {
                    res.bugs.Add(id + ": completed EARLY at action " + doneActions + "/" + totalActions
                                 + " (" + s.reagent.chemicalName + ") — a threshold or step-set is wrong");
                    log.AppendLine("  ✗ completed early at " + doneActions + "/" + totalActions);
                    return false;
                }
            }
            // The pour happened; did the BINDING hear it? Silent-unsubscribed is
            // exactly the bug that hard-stuck the 2026-07-17 headset run.
            if (s.requiredMl > 0f && b.AccumulatedFor(id, s.reagent) <= before + 0.001f
                && !runner.Graph.IsComplete(id))
            {
                res.bugs.Add(id + ": poured " + s.reagent.chemicalName + " into " + b.name
                             + " but the binding COUNTED NOTHING (IsListening=" + b.IsListening
                             + ") — the event chain is broken");
                log.AppendLine("  ✗ binding heard nothing");
            }

            demand.TryGetValue(s.reagent, out float d); demand[s.reagent] = d + Mathf.Max(inc * n, 0f);
            pouredInto.TryGetValue(b, out float pv); pouredInto[b] = pv + inc * n;
            log.AppendLine("  " + n + "× " + VerbFor(s) + " " + s.reagent.chemicalName
                           + " (from " + src.name + ") → " + b.name
                           + " (" + (inc * n).ToString("0.#") + UnitFor(s) + ")");
        }
        return true;
    }

    /// The filter pour, played honestly: source = a liquid-holding vessel bound
    /// to one of the filter task's PREREQUISITE tasks (the boiled tube);
    /// destination = the vessel bound to a task that lists the filter task as
    /// its prerequisite (the beaker the test happens in). Transferred in real
    /// 0.5 ml PourOut→AddLiquid ticks, exactly like a tilt through the funnel.
    static void SimulateFunnelPour(ExperimentRunner runner, string filterTaskId, StringBuilder log)
    {
        ExperimentTask filterTask = null;
        foreach (var t in runner.Graph.Tasks) if (t.taskId == filterTaskId) filterTask = t;
        if (filterTask == null) return;

        var bindings = Object.FindObjectsByType<LiquidTaskBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        LiquidPhysics src = null, dst = null;
        foreach (var b in bindings)
        {
            var lp = b.GetComponent<LiquidPhysics>();
            if (lp == null) continue;
            foreach (var s in b.ExpectedSteps)
            {
                if (s == null) continue;
                if (filterTask.prerequisites.Contains(s.taskId) && lp.currentLiquidVolume > 1f) src = lp;
                foreach (var t in runner.Graph.Tasks)
                    if (t.taskId == s.taskId && t.prerequisites.Contains(filterTaskId)) dst = lp;
            }
        }
        if (src == null || dst == null)
        {
            log.AppendLine("  (no funnel pour modelled: src=" + (src != null ? src.name : "—")
                           + " dst=" + (dst != null ? dst.name : "—") + ")");
            return;
        }
        float moved = 0f;
        for (int i = 0; i < 400 && src.currentLiquidVolume > 0.5f; i++)
        {
            var chem = src.PourOut(0.5f);
            if (chem == null) break;
            dst.AddLiquid(chem, 0.5f);
            moved += 0.5f;
        }
        log.AppendLine("  poured " + moved.ToString("0.#") + " ml of "
                       + (dst.currentChemical != null ? dst.currentChemical.chemicalName : "filtrate")
                       + " through the funnel: " + src.name + " → " + dst.name);
    }

    /// The bench bottle a player would pour from: holds the chemical, is not a
    /// task vessel, has the most left. Mirrors the dropper's own source rule.
    static LiquidPhysics FindSource(ChemicalData chem)
    {
        LiquidPhysics best = null;
        foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (lp != null && lp.currentChemical == chem && lp.currentLiquidVolume > 0.01f
                && lp.GetComponent<LiquidTaskBinding>() == null && !lp.name.StartsWith("Vessel_")
                && (best == null || lp.currentLiquidVolume > best.currentLiquidVolume))
                best = lp;
        return best;
    }

    /// Sustained-verb (sim) and drop-zone stations. Returns false when no station
    /// owns the task at all.
    static bool SimulateStation(ExperimentRunner runner, string id, ZoneSimStation[] sims,
        ExperimentTaskStation[] zones, TemperatureSim[] temps, LabItem[] labItems,
        Result res, StringBuilder log)
    {
        foreach (var st in sims)
        {
            if (st == null || st.TaskId != id) continue;
            // FILTER stations: the player POURS the previous step's vessel
            // through the funnel into the receiver below (LiquidPassthrough lets
            // the stream through). Model that pour for real — the receiving
            // vessel must actually hold the filtrate or the follow-up chemical
            // test has nothing to react with (the FeCl3 violet stayed invisible
            // until this was simulated honestly, 2026-07-17).
            if (st.Kind == StationSim.Filter)
                SimulateFunnelPour(runner, id, log);
            // Heat stations gate on a LIT burner (ignition gate) — the player's
            // match-strike becomes an Ignite() call here. Snuffed after, so the
            // scene isn't left with burning burners.
            var burners = Object.FindObjectsByType<BurnerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (st.Kind == StationSim.Heat)
            {
                foreach (var bu in burners) if (bu != null) bu.Ignite();
                log.AppendLine("  (lit the burner)");
            }
            // Occupied + lit: 300 simulated seconds at 2 Hz.
            for (int i = 0; i < 600 && !runner.Graph.IsComplete(id); i++)
            {
                st.Drive(0.5f, true);
                foreach (var ts in temps) if (ts != null) ts.Tick(0.5f);
                runner.Graph.Tick();
            }
            // Vacate the zone (the player takes the vessel out to move on) — one
            // unoccupied Drive latches heating OFF, else the bath keeps cooking
            // through the NEXT task's sim ticks and logs a phantom Overheat.
            st.Drive(0.5f, false);
            if (st.Kind == StationSim.Heat)
                foreach (var bu in burners) if (bu != null) bu.Extinguish();
            if (runner.Graph.IsComplete(id))
                log.AppendLine("  ✓ " + st.Kind + " sim ran to target (station occupied)");
            else
            {
                res.bugs.Add(id + ": " + st.Kind + " station never reached its target in 300 sim-seconds");
                log.AppendLine("  ✗ sim stalled");
                runner.CompleteTask(id);   // force past to keep exploring downstream
            }
            return true;
        }
        foreach (var st in zones)
        {
            if (st == null || st.TaskId != id) continue;
            bool served = string.IsNullOrEmpty(st.RequiredItemId);
            foreach (var li in labItems) if (st.AcceptsItem(li)) { served = true; break; }
            if (!served)
            {
                res.bugs.Add(id + ": station wants item '" + st.RequiredItemId + "' but NOTHING on the bench has that itemId");
                log.AppendLine("  ✗ required item missing from the scene");
                runner.CompleteTask(id);
            }
            else
            {
                st.Activate();
                log.AppendLine("  ✓ zone station activated"
                               + (string.IsNullOrEmpty(st.RequiredItemId) ? "" : " (bench has '" + st.RequiredItemId + "')"));
            }
            return true;
        }
        return false;
    }

    /// The run REALLY drained the bottles (PourOut per action), so sufficiency
    /// was proven by playing: a dry source mid-step already warned. This audit
    /// reports what the full run left in each bottle — a small remainder is the
    /// early starvation signal for a player who wastes a few pours.
    static void AuditSupplies(Dictionary<ChemicalData, float> demand, LabItem[] labItems,
                              Result res, StringBuilder log)
    {
        log.AppendLine("\n--- supply audit (post-run: consumed vs left in the bottle) ---");
        var sources = Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var kv in demand)
        {
            LiquidPhysics best = null;
            foreach (var lp in sources)
                if (lp != null && lp.currentChemical == kv.Key
                    && (best == null || lp.currentLiquidVolume > best.currentLiquidVolume)
                    && !lp.name.StartsWith("Vessel_") && lp.GetComponent<LiquidTaskBinding>() == null)
                    best = lp;   // a task vessel is a destination, never the shelf supply
            if (best == null)
                res.bugs.Add("no bench source holds '" + kv.Key.chemicalName + "' (run consumed "
                             + kv.Value.ToString("0.#") + ") — the player cannot obtain it");
            else if (best.currentLiquidVolume < kv.Value * 0.5f)
                res.warnings.Add("'" + kv.Key.chemicalName + "': one perfect run left only "
                                 + best.currentLiquidVolume.ToString("0.#") + " in " + best.name
                                 + " — little margin for a wasteful player");
            log.AppendLine("  " + kv.Key.chemicalName + ": consumed " + kv.Value.ToString("0.#")
                           + (best == null ? "  ✗ NO SOURCE" : "  · " + best.name + " has " + best.currentLiquidVolume.ToString("0.#") + " left"));
        }
    }

    /// A correct delivery the player cannot SEE reads as a bug in VR (the 0.5%
    /// bucket-fill lesson) — flag any task vessel that ENDS the run under 4% of
    /// its capacity. Reads the LIVE vessel (funnel pours and reactions included),
    /// not the scripted-delivery tally, which under-counted the filtrate beaker.
    static void AuditVisibility(Dictionary<LiquidTaskBinding, float> pouredInto,
                                Result res, StringBuilder log)
    {
        foreach (var kv in pouredInto)
        {
            var lp = kv.Key != null ? kv.Key.GetComponent<LiquidPhysics>() : null;
            if (lp == null || lp.maxVolume <= 0f) continue;
            float end = lp.currentLiquidVolume + lp.currentPptVolume;
            float frac = end / lp.maxVolume;
            if (frac > 0f && frac < 0.04f)
                res.warnings.Add(kv.Key.name + ": ends the run at " + (frac * 100f).ToString("0.#")
                                 + "% of capacity (" + end.ToString("0.#") + "/" + lp.maxVolume + " ml) — invisible in VR");
        }
    }

    // ---- the VERB CONTRACT, mirrored ---------------------------------------
    static bool IsSolid(LiquidTaskBinding.ReagentStep s)
        => s.reagent != null && (s.reagent.state == PhysicalState.Solid || s.reagent.state == PhysicalState.Powder);
    static float IncrementFor(LiquidTaskBinding.ReagentStep s)
        => IsSolid(s) ? ScoopMath.GramsPerSpatula : DropperMath.MlPerSqueeze;
    static int ActionsFor(LiquidTaskBinding.ReagentStep s)
        => s.requiredMl <= 0f ? 1 : Mathf.CeilToInt(s.requiredMl / IncrementFor(s) - 0.0001f);
    static string VerbFor(LiquidTaskBinding.ReagentStep s) => IsSolid(s) ? "spatula-dip" : "squeeze/pour";
    static string UnitFor(LiquidTaskBinding.ReagentStep s) => IsSolid(s) ? " g" : " ml";
}
#endif
