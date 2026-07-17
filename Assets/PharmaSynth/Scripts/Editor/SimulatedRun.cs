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
/// HOW it simulates: builder.Build() wires the real scene (bench-bound vessels
/// included), runner.StartExperiment() opens the real graph, then each available
/// task is completed through its own mechanism — LiquidTaskBinding.HandleReagent
/// per VERB-CONTRACT action (1 ml a squeeze, 0.1 g a spatula dip), RackTaskGroup
/// .ShouldFire after the set is served, ZoneSimStation.Drive + TemperatureSim
/// .Tick for sustained verbs, ExperimentTaskStation.Activate for zone drops.
/// Edit-mode AddComponent fires no OnEnable, so the LiquidPhysics→binding event
/// hop is NOT exercised here — that hop is play-mode-only and stays on the
/// headset checklist.
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

        // Leave the scene as a plain revealed stage: a fresh Build clears the
        // sim-mutated binding state (bench bindings are stripped + re-added).
        runner.Mistakes.MistakeRecorded -= onMistake;
        builder.Build(moduleId);
        return res;
    }

    /// Deliver every reagent step of one task in VERB-CONTRACT increments,
    /// checking the thresholds behave: never complete before the final action.
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
            float inc = IncrementFor(s);
            int n = ActionsFor(s);
            for (int k = 0; k < n; k++)
            {
                b.HandleReagent(s.reagent, inc);
                doneActions++;
                if (runner.Graph.IsComplete(id) && doneActions < totalActions)
                {
                    res.bugs.Add(id + ": completed EARLY at action " + doneActions + "/" + totalActions
                                 + " (" + s.reagent.chemicalName + ") — a threshold or step-set is wrong");
                    log.AppendLine("  ✗ completed early at " + doneActions + "/" + totalActions);
                    return false;
                }
            }
            demand.TryGetValue(s.reagent, out float d); demand[s.reagent] = d + Mathf.Max(inc * n, 0f);
            pouredInto.TryGetValue(b, out float pv); pouredInto[b] = pv + inc * n;
            log.AppendLine("  " + n + "× " + VerbFor(s) + " " + s.reagent.chemicalName
                           + " → " + b.name + " (" + (inc * n).ToString("0.#") + UnitFor(s) + ")");
        }
        return true;
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

    /// Every chemical the run consumed must have a real source on the bench with
    /// enough in it — this is the "if apparatus we need is missing, TELL me" rule
    /// applied to reagents, and the starvation check before the player hits it.
    static void AuditSupplies(Dictionary<ChemicalData, float> demand, LabItem[] labItems,
                              Result res, StringBuilder log)
    {
        log.AppendLine("\n--- supply audit ---");
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
                res.bugs.Add("no bench source holds '" + kv.Key.chemicalName + "' (run needs "
                             + kv.Value.ToString("0.#") + ") — the player cannot obtain it");
            else if (best.currentLiquidVolume < kv.Value)
                res.warnings.Add("'" + kv.Key.chemicalName + "': source " + best.name + " holds "
                                 + best.currentLiquidVolume.ToString("0.#") + " but the run needs "
                                 + kv.Value.ToString("0.#") + " — starvation before the last step");
            log.AppendLine("  " + kv.Key.chemicalName + ": need " + kv.Value.ToString("0.#")
                           + (best == null ? "  ✗ NO SOURCE" : "  ← " + best.name + " (" + best.currentLiquidVolume.ToString("0.#") + ")"));
        }
    }

    /// A correct delivery the player cannot SEE reads as a bug in VR (the 0.5%
    /// bucket-fill lesson) — flag any vessel whose full scripted intake stays
    /// under 4% of its capacity.
    static void AuditVisibility(Dictionary<LiquidTaskBinding, float> pouredInto,
                                Result res, StringBuilder log)
    {
        foreach (var kv in pouredInto)
        {
            var lp = kv.Key != null ? kv.Key.GetComponent<LiquidPhysics>() : null;
            if (lp == null || lp.maxVolume <= 0f) continue;
            float frac = kv.Value / lp.maxVolume;
            if (frac > 0f && frac < 0.04f)
                res.warnings.Add(kv.Key.name + ": full scripted intake is " + (frac * 100f).ToString("0.#")
                                 + "% of capacity (" + kv.Value.ToString("0.#") + "/" + lp.maxVolume + " ml) — invisible in VR");
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
