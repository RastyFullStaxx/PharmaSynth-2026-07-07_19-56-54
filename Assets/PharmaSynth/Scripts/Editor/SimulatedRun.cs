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

        // The module's own END PRODUCT is hidden from the bench during its run
        // ("the player must craft it") — a shelf bottle of it therefore CANNOT
        // be a source, however the edit-mode scene looks. Round one poured the
        // litmus sample straight from the (play-mode-hidden) Raw_BenzoicAcid.
        s_hiddenProduct = EndProductVisibility.HiddenProductFor(moduleId, false);

        var res = new Result { totalTasks = module.graphTasks.Count };
        log.AppendLine("=== Simulated run: " + moduleId + " (" + res.totalTasks + " tasks) ===");

        // Every mistake as it lands, in context — a count alone can't tell a
        // mis-authored binding from a fume-hood rule firing on a bench pour.
        System.Action<LabErrorType, string> onMistake =
            (type, msg) => log.AppendLine("  ⚠ MISTAKE [" + type + "] " + msg);
        runner.Mistakes.MistakeRecorded += onMistake;

        // Heat-gate watchdog (user 2026-07-17: "achieve the needed in the
        // procedure first, before these reactions come"): any temperature-gated
        // rule that fires while its vessel is still COLD is a broken gate.
        var watchers = new List<(LiquidPhysics lp, System.Action<ReactionRule> h)>();
        foreach (var wlp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var cap = wlp;
            System.Action<ReactionRule> h = rule =>
            {
                if (rule != null && !rule.TemperatureSatisfied(cap.currentTempC))
                    res.bugs.Add(cap.name + ": " + rule.name + " fired at " + cap.currentTempC.ToString("0")
                                 + " C but needs " + rule.minTemperatureC.ToString("0") + " C — the heat gate is broken");
            };
            cap.ReactionOccurred += h;
            watchers.Add((cap, h));
        }

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
            // Reconcile first: a wrap-up ("record observations") or a condition
            // task can auto-complete via Graph.Tick DURING another task's sim
            // (e.g. the ester heat loop ticks the graph). Count it here or the
            // walk false-deadlocks — it's already complete, so never "available".
            foreach (var gt in runner.Graph.Tasks)
                if (runner.Graph.IsComplete(gt.taskId) && !done.Contains(gt.taskId))
                { done.Add(gt.taskId); res.completedTasks++; progressed = true;
                  log.AppendLine("\n» " + gt.taskId + " — auto-completed (wrap-up / condition)"); }

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
                        else if (SimulateVesselHeat(runner, id, steps, res, log)) { }
                        else if (SimulateFermentation(runner, id, steps, res, log)) { }
                        else if (SimulateLitmus(runner, id, steps, res, log)) { }
                        else if (SimulateStation(runner, id, simStations, zoneStations, temps, labItems, res, log)) { }
                        else if (deferred)
                        {
                            res.bugs.Add(id + ": steps defer completion (completesTask:false) but NO rack group, heat task, or station owns it");
                            log.AppendLine("  ✗ deferred but ownerless");
                        }
                        else
                            res.bugs.Add(id + ": every step delivered its full amount yet the task did not complete");
                    }
                    else if (runner.Graph.IsComplete(id))
                        log.AppendLine("  ✓ completed by delivery");
                    // The procedure's heat step, played: mixes held cold go into
                    // the water bath and must fire there.
                    if (runner.Graph.IsComplete(id)) WarmAtBath(id, steps, res, log);
                }
                else
                    handled = SimulateStation(runner, id, simStations, zoneStations, temps, labItems, res, log)
                              || SimulateChillTask(runner, id, res, log);

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
        foreach (var (wlp, wh) in watchers) if (wlp != null) wlp.ReactionOccurred -= wh;
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
        // PLAN first (source, increment, action count per step) so the early-
        // completion check compares against the REAL minimum — planning dips but
        // executing tilt-pours made 10/10 look like 10/50; overshoot pours are
        // OPTIONAL (a player stops when the ✓ shows), so only completion BELOW
        // the minimum is a bug.
        var plan = new List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s, LiquidPhysics src, bool vesselSource, bool scoopDraw, float inc, int n)>();
        int minTotal = 0;
        foreach (var (b, s) in steps)
        {
            var plp = b.GetComponent<LiquidPhysics>();
            if (plp == null)
            {
                res.bugs.Add(id + ": binding for " + s.reagent.chemicalName + " sits on '" + b.name
                             + "' which has NO LiquidPhysics — nothing to pour into");
                return false;
            }
            var psrc = FindSource(s.reagent, plp);
            if (psrc == null)
            {
                res.bugs.Add(id + ": no bench source holds '" + s.reagent.chemicalName
                             + "' — the player cannot perform this step");
                log.AppendLine("  ✗ no source bottle for " + s.reagent.chemicalName);
                continue;   // supply audit details it; keep walking
            }
            // Pouring FROM another vessel (the filtrate through the funnel) is a
            // tilt-pour of the solution — never spatula dips, whatever the
            // chemical's dry state says (round one dipped the hydrolysate 50×).
            // EXCEPT crystals (user 2026-07-18: "if it's solid, shouldn't we use
            // porcelain spatula or scoopula?"): a solid-state product drawn from
            // the CHILL (crystallising) flask is a scoop dip — heat vessels hold
            // liquid filtrates/distillates and stay pours.
            bool pv = psrc.GetComponent<LiquidTaskBinding>() != null;
            bool scoopDraw = pv && IsSolid(s) && psrc.GetComponent<VesselChillTask>() != null;
            float pinc = pv && !scoopDraw ? 0.5f : IncrementFor(s);
            int pmin = s.requiredMl <= 0f ? 1 : Mathf.CeilToInt(s.requiredMl / pinc - 0.0001f);
            int pn = pmin;
            // Human overshoot on bulk tilt-pours only — squeezes and dips are counted.
            if (((pv && !scoopDraw) || !IsSolid(s)) && s.requiredMl > 4f) pn = Mathf.CeilToInt(pn * 1.2f);
            plan.Add((b, s, psrc, pv, scoopDraw, pinc, pn));
            minTotal += pmin;
        }

        int doneActions = 0;

        foreach (var (b, s, src, vesselSource, scoopDraw, inc, n) in plan)
        {
            var lp = b.GetComponent<LiquidPhysics>();

            float before = b.AccumulatedFor(id, s.reagent);
            int poured = 0;
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
                doneActions++; poured++;
                if (runner.Graph.IsComplete(id))
                {
                    if (doneActions < minTotal)
                    {
                        res.bugs.Add(id + ": completed EARLY at action " + doneActions + "/" + minTotal
                                     + " minimum (" + s.reagent.chemicalName + ") — a threshold or step-set is wrong");
                        log.AppendLine("  ✗ completed early at " + doneActions + "/" + minTotal);
                        return false;
                    }
                    break;   // the ✓ showed — a player stops pouring
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

            // Only SHELF draws feed the supply audit — a vessel-to-vessel pour
            // (the filtrate, the product draws) consumes nothing from a bottle.
            if (!vesselSource)
            { demand.TryGetValue(s.reagent, out float d); demand[s.reagent] = d + Mathf.Max(inc * poured, 0f); }
            pouredInto.TryGetValue(b, out float pv); pouredInto[b] = pv + inc * poured;
            string verb = scoopDraw ? "scoopula-dip (crystals)"
                        : vesselSource ? "tilt-pour (vessel to vessel)" : VerbFor(s);
            log.AppendLine("  " + poured + "× " + verb
                           + " " + s.reagent.chemicalName + " (from " + src.name + ") → " + b.name
                           + " (" + (inc * poured).ToString("0.#") + (vesselSource && !scoopDraw ? " ml" : UnitFor(s)) + ")");
        }
        return true;
    }

    /// The REAL water bath, prepared like a player: pour distilled water into it
    /// from the shelf bottle (once), stand a lit burner beside it, let it heat.
    /// Returns the ready controller, or null with a bug when the tool chain is
    /// broken. The bath is the only heat source — no zones, no stations.
    static WaterBathController PrepareBath(Result res, StringBuilder log)
    {
        var bath = Object.FindAnyObjectByType<WaterBathController>();
        if (bath == null)
        {
            res.bugs.Add("no WaterBathController in the scene — heated steps are unplayable (run Apply W5.8 Verb Data)");
            return null;
        }
        if (!bath.HasWater)
        {
            var bathLp = bath.GetComponentInChildren<LiquidPhysics>();
            ChemicalData water = null;
            foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (lp != null && lp.currentChemical != null && lp.currentChemical.chemicalName == "Distilled Water"
                    && lp.GetComponent<LiquidTaskBinding>() == null)
                { water = lp.currentChemical;
                  for (int i = 0; i < 20; i++) { var c = lp.PourOut(1f); if (c == null) break; bathLp.AddLiquid(c, 1f); }
                  break; }
            if (water == null || !bath.HasWater)
            {
                res.bugs.Add("could not fill the water bath with Distilled Water — bath unusable");
                return null;
            }
            log.AppendLine("  (filled the water bath with 20 ml Distilled Water)");
        }
        log.AppendLine("  (lit a burner beside the bath)");
        return bath;
    }

    /// A mixed-cold recipe carried to the bath: heat until it fires (or the bath
    /// tops out — then the gate itself is broken).
    static void WarmAtBath(string id,
        List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)> steps, Result res, StringBuilder log)
    {
        var seen = new HashSet<LiquidPhysics>();
        foreach (var (b, _) in steps)
        {
            var lp = b != null ? b.GetComponent<LiquidPhysics>() : null;
            if (lp == null || !seen.Add(lp) || !lp.HasPendingReaction) continue;
            var rule = lp.PendingRule;
            var bath = PrepareBath(res, log);
            if (bath == null) return;
            for (int i = 0; i < 600 && lp.HasPendingReaction; i++)
            {
                bath.DriveForTest(0.5f);
                bath.HeatVessel(lp);
            }
            if (!lp.HasPendingReaction)
                log.AppendLine("  ✓ held " + lp.name + " in the bath (" + bath.BathC.ToString("0")
                               + " C) → " + rule.name
                               + (string.IsNullOrEmpty(rule.expectedObservation) ? "" : " (" + rule.expectedObservation + ")"));
            else
                res.bugs.Add(id + ": bath topped out at " + bath.BathC.ToString("0") + " C but "
                             + rule.name + " (needs " + rule.minTemperatureC.ToString("0") + " C) did not fire");
        }
    }

    /// The zone-free FERMENTATION step (Exp 3): the must is prepared, the player
    /// leads the delivery tube from the flask into a limewater tube; CO₂ clouds
    /// it (CaCO₃) and the task completes — then the longProcess time-skip fires
    /// ("one week later"). Played by bubbling CO₂ into the limewater vessel this
    /// task's binding names, until the FermentationController's condition trips.
    static bool SimulateFermentation(ExperimentRunner runner, string id,
        List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)> steps, Result res, StringBuilder log)
    {
        FermentationController fc = null;
        foreach (var f in Object.FindObjectsByType<FermentationController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (f != null && f.FermentTaskId == id) { fc = f; break; }
        if (fc == null) return false;

        // The limewater vessel this task fills (its binding holds limewater).
        LiquidPhysics lime = null;
        foreach (var (b, s) in steps)
        {
            var lp = b != null ? b.GetComponent<LiquidPhysics>() : null;
            if (lp != null && fc.Limewater != null && lp.currentChemical == fc.Limewater) { lime = lp; break; }
        }
        if (lime == null)
        {
            res.bugs.Add(id + ": fermentation has no limewater vessel to cloud (pour limewater into a tube first)");
            return true;
        }
        if (!fc.Fermenting)
        {
            res.bugs.Add(id + ": flask is not fermenting (is prepare-must complete + the must in the flask?)");
            return true;
        }
        for (int i = 0; i < 200 && !runner.Graph.IsComplete(id); i++)
        {
            fc.BubbleInto(lime);
            runner.Graph.Tick();
        }
        if (runner.Graph.IsComplete(id))
            log.AppendLine("  ✓ led the delivery tube in — limewater clouded (CaCO₃), fermentation confirmed → time-skip");
        else
        {
            res.bugs.Add(id + ": bubbled CO₂ but the limewater never clouded / task never completed");
            runner.CompleteTask(id);
        }
        return true;
    }

    /// The zone-free heat STEP: the vessel owning a VesselHeatTask is served,
    /// then held in the bath until its task condition (served AND hot) trips.
    static bool SimulateVesselHeat(ExperimentRunner runner, string id,
        List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)> steps, Result res, StringBuilder log)
    {
        VesselHeatTask heat = null; LiquidPhysics tube = null;
        foreach (var (b, _) in steps)
        {
            var h = b != null ? b.GetComponent<VesselHeatTask>() : null;
            if (h != null && h.TaskId == id) { heat = h; tube = b.GetComponent<LiquidPhysics>(); break; }
        }
        if (heat == null || tube == null) return false;

        var bath = PrepareBath(res, log);
        if (bath == null) return true;   // owned, but the tool chain is broken (bug already logged)
        for (int i = 0; i < 600 && !runner.Graph.IsComplete(id); i++)
        {
            bath.DriveForTest(0.5f);
            bath.HeatVessel(tube);
            runner.Graph.Tick();
        }
        if (runner.Graph.IsComplete(id))
            log.AppendLine("  ✓ held " + tube.name + " in the bath to " + heat.RequiredC.ToString("0")
                           + " C — step complete (tube at " + tube.currentTempC.ToString("0") + " C)");
        else
        {
            res.bugs.Add(id + ": bath heating never completed the step (tube " + tube.currentTempC.ToString("0")
                         + " C / needs " + heat.RequiredC.ToString("0") + " C, bath " + bath.BathC.ToString("0") + " C)");
            runner.CompleteTask(id);   // force past to keep exploring downstream
        }
        return true;
    }

    /// The zone-free chill STEP (Exp 4 crystallise): the flask holding the
    /// acidified product is set in the real ice bucket until its task condition
    /// (holding something AND cold) trips. No binding of its own — the vessel is
    /// found by its VesselChillTask.
    static bool SimulateChillTask(ExperimentRunner runner, string id, Result res, StringBuilder log)
    {
        VesselChillTask chill = null;
        foreach (var c in Object.FindObjectsByType<VesselChillTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.TaskId == id) { chill = c; break; }
        if (chill == null) return false;

        var lp = chill.GetComponent<LiquidPhysics>();
        var ice = Object.FindAnyObjectByType<IceBathController>();
        if (ice == null)
        {
            res.bugs.Add(id + ": no IceBathController in the scene — chill steps are unplayable (run Apply W5.8 Verb Data)");
            runner.CompleteTask(id);
            return true;
        }
        for (int i = 0; i < 40 && !runner.Graph.IsComplete(id); i++)
        {
            ice.ChillVessel(lp);
            runner.Graph.Tick();
        }
        if (runner.Graph.IsComplete(id))
            log.AppendLine("  ✓ set " + (lp != null ? lp.name : "the vessel") + " in the ice bath (zone r="
                           + ice.ChillZoneRadius.ToString("0.00") + " m) — chilled to "
                           + (lp != null ? lp.currentTempC.ToString("0") : "?") + " C, crystals formed");
        else
        {
            res.bugs.Add(id + ": ice bath never completed the chill (vessel "
                         + (lp != null ? lp.currentTempC.ToString("0") + " C holding " + (lp.currentLiquidVolume + lp.currentPptVolume).ToString("0.#") + " ml" : "missing")
                         + " / needs <= " + chill.RequiredC.ToString("0") + " C with contents)");
            runner.CompleteTask(id);   // force past to keep exploring downstream
        }
        return true;
    }

    /// The zone-free LITMUS confirmation (Exp 4 test-litmus): once the tube is
    /// served, a fresh strip is touched to it through the strip's real read path
    /// (TouchVessel — the physics-free stand-in for the dip). The mixture must
    /// actually read acid or the strip stays violet and the task must not pass.
    static bool SimulateLitmus(ExperimentRunner runner, string id,
        List<(LiquidTaskBinding b, LiquidTaskBinding.ReagentStep s)> steps, Result res, StringBuilder log)
    {
        VesselLitmusTask lt = null; LiquidPhysics tube = null;
        foreach (var (b, _) in steps)
        {
            var l = b != null ? b.GetComponent<VesselLitmusTask>() : null;
            if (l != null && l.TaskId == id) { lt = l; tube = b.GetComponent<LiquidPhysics>(); break; }
        }
        if (lt == null) return false;

        // The player tears a strip off the bench litmus box — it must exist,
        // and at least one dispensable strip must CARRY the LitmusStrip
        // component with a collider, or a real touch can never register.
        bool boxOnBench = false;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Raw_LitmusPaper")) { boxOnBench = true; break; }
        if (!boxOnBench)
            res.bugs.Add(id + ": no Raw_LitmusPaper box on the bench — the player has no strip to test with");
        bool stripWired = false;
        foreach (var st in Object.FindObjectsByType<LitmusStrip>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (st != null && st.GetComponentInChildren<Collider>(true) != null) { stripWired = true; break; }
        if (!stripWired)
            res.bugs.Add(id + ": no litmus strip in the scene carries a Collider — a physical touch can never register in play");

        var stripGo = new GameObject("SimLitmusStrip");
        try
        {
            stripGo.AddComponent<LitmusStrip>().TouchVessel(tube);
            runner.Graph.Tick();
        }
        finally { Object.DestroyImmediate(stripGo); }

        if (runner.Graph.IsComplete(id))
            log.AppendLine("  ✓ touched a litmus strip to " + tube.name + " — mixture pH "
                           + tube.CurrentPH.ToString("0.#") + " read ACID, strip turned red");
        else
        {
            res.bugs.Add(id + ": the litmus touch did not complete the task (mixture pH "
                         + (tube != null ? tube.CurrentPH.ToString("0.#") : "?")
                         + ", red needs <= " + LitmusMath.AcidPH + ")");
            runner.CompleteTask(id);   // force past to keep exploring downstream
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

    /// The module's own hidden end product (null when it has none) — set per run.
    static string s_hiddenProduct;

    /// The source a player would pour from. A TASK VESSEL that already holds the
    /// chemical wins over the shelf — that IS the filtrate pour (tube 17's
    /// hydrolysate through the funnel into the beaker), the intended play; the
    /// shelf bottle is the fallback for fresh reagents.
    static LiquidPhysics FindSource(ChemicalData chem, LiquidPhysics destination)
    {
        LiquidPhysics product = null, vessel = null, wash = null, shelf = null;
        foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lp == null || lp == destination || lp.currentChemical != chem
                || lp.currentLiquidVolume <= 0.01f || lp.name.StartsWith("Vessel_")) continue;
            if (lp.GetComponent<LiquidTaskBinding>() != null)
            {
                // A bound vessel is a source ONLY when it is a SYNTHESIS vessel
                // (heat / chill / fermentation task) — the hydrolysate pour, the
                // decant. Round one drew "ethanol" out of the finished enol
                // tube; round two tipped the beaker's violet TEST RESIDUE into
                // the control tube — hence heat/chill also require the ledger
                // collapsed to ONE product entry.
                // The CHILL (crystallising) vessel is the FINISHED product and
                // beats any upstream heat vessel: Exp 4's tests must draw from
                // the purified flask, not the crude reaction beaker still full
                // of MnO2 sludge (round one did exactly that).
                if (lp.GetComponent<VesselChillTask>() != null && lp.Ledger.Count == 1)
                { if (product == null || lp.currentLiquidVolume > product.currentLiquidVolume) product = lp; }
                else if (lp.GetComponent<VesselHeatTask>() != null && lp.Ledger.Count == 1)
                { if (vessel == null || lp.currentLiquidVolume > vessel.currentLiquidVolume) vessel = lp; }
                // The FERMENTATION flask is the manuscript's WASH — decanted
                // into the distilling flask as a real mixture (its ledger still
                // lists yeast/nutrient/NaOH), so no single-entry requirement.
                // Lowest vessel priority: the distillate beats the crude wash.
                else if (lp.GetComponent<FermentationController>() != null)
                { if (wash == null || lp.currentLiquidVolume > wash.currentLiquidVolume) wash = lp; }
            }
            else
            {
                // A shelf bottle of the module's own END PRODUCT is hidden in
                // play — the player cannot pour from it, so neither may the sim.
                // (This exclusion is what exposed Exp 3's unplayable "wash =
                // bench Ethanol" shorthand: that bottle is hidden during Exp 3.)
                if (chem != null && chem.chemicalName == s_hiddenProduct) continue;
                if (shelf == null || lp.currentLiquidVolume > shelf.currentLiquidVolume) shelf = lp;
            }
        }
        return product != null ? product : vessel != null ? vessel : wash != null ? wash : shelf;
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
            // The boil itself: the bath at target heats the vessels bound to
            // this task, which is what fires their high-threshold pending
            // reactions (the aspirin hydrolysis needs 90 C — only here).
            if (st.Kind == StationSim.Heat)
                foreach (var b2 in Object.FindObjectsByType<LiquidTaskBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    foreach (var s2 in b2.ExpectedSteps)
                    {
                        if (s2 == null || s2.taskId != id) continue;
                        var vlp = b2.GetComponent<LiquidPhysics>();
                        if (vlp != null)
                        {
                            bool had = vlp.HasPendingReaction;
                            var pr = vlp.PendingRule;
                            vlp.SetTemperature(100f);
                            if (had && !vlp.HasPendingReaction)
                                log.AppendLine("  ✓ boil fired " + (pr != null ? pr.name : "the pending reaction")
                                               + " in " + vlp.name);
                            else if (had)
                                res.bugs.Add(id + ": boiled " + vlp.name + " to 100 C but "
                                             + (pr != null ? pr.name : "its reaction") + " still pending");
                        }
                        break;
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
                    && kv.Key.chemicalName != s_hiddenProduct   // hidden end product: not a supply
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
            // Judge the PEAK the player saw — a vessel deliberately emptied later
            // (the hydrolysis tube pours through the funnel) is not "invisible".
            float end = lp.currentLiquidVolume + lp.currentPptVolume;
            float peak = Mathf.Max(end, kv.Value);
            float frac = peak / lp.maxVolume;
            if (frac > 0f && frac < 0.04f)
                res.warnings.Add(kv.Key.name + ": never exceeds " + (frac * 100f).ToString("0.#")
                                 + "% of capacity (" + peak.ToString("0.#") + "/" + lp.maxVolume + " ml) — invisible in VR");
        }
    }

    // ---- the VERB CONTRACT, mirrored ---------------------------------------
    static bool IsSolid(LiquidTaskBinding.ReagentStep s)
        => s.reagent != null && (s.reagent.state == PhysicalState.Solid || s.reagent.state == PhysicalState.Powder);
    // A player picks the tool by scale: the SCOOPULA (2 g) for a bulk weigh-out
    // (Exp 3's 12 g sugar / 4 g yeast — 120 spatula dips would be absurd), the
    // fine porcelain SPATULA (0.1 g) only for sub-gram amounts (Exp 2's 0.1 g).
    static float IncrementFor(LiquidTaskBinding.ReagentStep s)
        => IsSolid(s) ? (s.requiredMl >= 2f ? ScoopMath.GramsPerScoop : ScoopMath.GramsPerSpatula)
                      : DropperMath.MlPerSqueeze;
    static int ActionsFor(LiquidTaskBinding.ReagentStep s)
        => s.requiredMl <= 0f ? 1 : Mathf.CeilToInt(s.requiredMl / IncrementFor(s) - 0.0001f);
    static string VerbFor(LiquidTaskBinding.ReagentStep s)
        => IsSolid(s) ? (s.requiredMl >= 2f ? "scoopula-dip" : "spatula-dip") : "squeeze/pour";
    static string UnitFor(LiquidTaskBinding.ReagentStep s) => IsSolid(s) ? " g" : " ml";
}
#endif
