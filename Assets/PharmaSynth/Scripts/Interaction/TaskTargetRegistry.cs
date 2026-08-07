using System.Collections.Generic;
using UnityEngine;

/// What role an object plays in a step — drives the guidance tint and whether
/// grabbing it should silence the glow.
public enum TargetRole { Source, Destination, Tool, Station }

/// One highlightable object for one task.
public struct TaskTarget
{
    public Transform transform;
    public TargetRole role;

    /// Source bottles go quiet once in hand (you got the message). Destinations
    /// and tools stay lit while held — "this is the right tube" is still the
    /// answer while you're carrying it.
    public bool stayLitWhenHeld;
}

/// taskId → the scene objects that step involves.
///
/// Formerly ExperimentStationRegistry, which mapped ONE Transform per task and was
/// fed only by ExperimentTaskStation.OnEnable. Since the zone-free conversion
/// (2026-07-17) no module stages a station, so nothing ever registered and every
/// consumer silently got null — WaypointGuide has been calling Hide() every frame in
/// all 9 modules ever since. Widened to a list and fed by the TutorialTargets sweep,
/// which is the single source of truth: components no longer self-register, so there
/// is no second lifetime to keep in sync.
public static class TaskTargetRegistry
{
    private static readonly Dictionary<string, List<TaskTarget>> _map =
        new Dictionary<string, List<TaskTarget>>();

    public static void Register(string taskId, Transform t, TargetRole role, bool stayLitWhenHeld)
    {
        if (string.IsNullOrEmpty(taskId) || t == null) return;
        if (!_map.TryGetValue(taskId, out var list))
        {
            list = new List<TaskTarget>();
            _map[taskId] = list;
        }
        for (int i = 0; i < list.Count; i++)
            if (list[i].transform == t) return;              // idempotent: sweeps may overlap
        list.Add(new TaskTarget { transform = t, role = role, stayLitWhenHeld = stayLitWhenHeld });
    }

    /// Live targets for a step. Null transforms are filtered on every READ, not just
    /// on build — a vessel can break and be destroyed mid-run.
    public static IReadOnlyList<TaskTarget> Targets(string taskId)
    {
        if (string.IsNullOrEmpty(taskId) || !_map.TryGetValue(taskId, out var list))
            return System.Array.Empty<TaskTarget>();
        list.RemoveAll(e => e.transform == null);
        return list;
    }

    public static int TaskCount => _map.Count;

    public static void Clear() => _map.Clear();
}

/// Builds the taskId → objects map by sweeping the live scene once per run.
///
/// Deliberately DERIVED from the components that actually complete each task rather
/// than from an authored per-task list. An authored list can drift from the binding
/// it describes — exactly the bug class the W5.34 clueless-player audit was spent on
/// (a hint whose ACTION line contradicted the binding it had to satisfy). Deriving
/// makes that disagreement structurally impossible: if the sweep points at it, it is
/// the thing that completes the step.
public static class TutorialTargets
{
    /// Tasks in the last audited module that nothing in the scene claimed.
    public static readonly List<string> LastUnresolved = new List<string>();

    public static void Build()
    {
        TaskTargetRegistry.Clear();

        // --- Pours and scoops. The binding lives on the DESTINATION vessel and names
        //     the SOURCE chemical, so one entry yields both ends of the step.
        //     ScoopController deliberately has no taskId of its own: it completes via
        //     AddLiquid on the target vessel, so it is covered here rather than below.
        var vessels = Object.FindObjectsByType<LiquidPhysics>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in Object.FindObjectsByType<LiquidTaskBinding>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null) continue;
            var steps = b.ExpectedSteps;
            if (steps == null) continue;
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null || string.IsNullOrEmpty(step.taskId)) continue;
                TaskTargetRegistry.Register(step.taskId, b.transform, TargetRole.Destination, true);
                if (step.reagent == null) continue;
                foreach (var v in vessels)
                    if (v != null && v.currentChemical == step.reagent && v.transform != b.transform)
                        TaskTargetRegistry.Register(step.taskId, v.transform, TargetRole.Source, false);
            }
        }

        // --- Verb components: each already owns the taskId it satisfies.
        //
        //     The zone-free conversion (2026-07-17) put the taskId on a per-VESSEL
        //     companion (VesselHeatTask, VesselChillTask, …) rather than on the
        //     apparatus controller — the bath/bucket/strip are shared bench tools and
        //     cannot know whose step is running. So sweep the Vessel*Task components:
        //     they sit on the exact glassware the step is about, which is also what
        //     the player needs pointed out.
        RegisterVerb<GrindController>(c => c.TaskId);
        RegisterVerb<StirController>(c => c.TaskId);
        RegisterVerb<VesselHeatTask>(c => c.TaskId);
        RegisterVerb<VesselChillTask>(c => c.TaskId);
        RegisterVerb<VesselLitmusTask>(c => c.TaskId);
        RegisterVerb<VesselFlameTask>(c => c.TaskId);
        RegisterVerb<VesselWeighTask>(c => c.TaskId);
        RegisterVerb<VaporCollectController>(c => c.VaporTaskId);
        RegisterVerb<ZoneSimStation>(c => c.TaskId);
        RegisterVerb<RackTaskGroup>(c => c.TaskId);
        RegisterVerb<WeighStation>(c => c.TaskId);
        RegisterVerb<FermentationController>(c => c.FermentTaskId);
        RegisterVerb<ExperimentTaskStation>(c => c.TaskId);
        // (FermentationController.mustTaskId is deliberately NOT swept: it is a
        //  prerequisite the controller READS, not a step it completes — the juice
        //  pour's own LiquidTaskBinding already owns that target.)

        // --- The methane tutorial stages no dynamic vessels and hard-codes its four
        //     task ids inside the rig, so it cannot be swept generically. The rig
        //     publishes its own map (see MethaneApparatusRig.RegisterTutorialTargets)
        //     and we call it here, keeping ONE registry lifetime.
        foreach (var rig in Object.FindObjectsByType<MethaneApparatusRig>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rig != null) rig.RegisterTutorialTargets();

        // --- A station that demands a specific prop also points at the prop itself.
        //     NOTE: ZoneItemSensor is NOT a source here — it is a continuous-occupancy
        //     sensor for rigs (burner-in-zone, tube-in-place) and carries no taskId at
        //     all, so it cannot say which STEP a tool belongs to.
        var items = Object.FindObjectsByType<LabItem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var st in Object.FindObjectsByType<ExperimentTaskStation>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (st == null || string.IsNullOrEmpty(st.TaskId) || string.IsNullOrEmpty(st.RequiredItemId)) continue;
            foreach (var it in items)
                if (it != null && it.itemId == st.RequiredItemId)
                    TaskTargetRegistry.Register(st.TaskId, it.transform, TargetRole.Tool, true);
        }
    }

    private static void RegisterVerb<T>(System.Func<T, string> taskIdOf) where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;
            string id = taskIdOf(c);
            if (!string.IsNullOrEmpty(id))
                TaskTargetRegistry.Register(id, c.transform, TargetRole.Station, true);
        }
    }

    /// Records which of a module's tasks nothing in the scene claimed. A wrap-up step
    /// (autoCompleteWhenOthersDone) legitimately has no physical target and is skipped;
    /// anything else is an authoring gap and should be surfaced loudly, because the
    /// player would be told to act with nothing to point at.
    public static void AuditAgainst(IEnumerable<ExperimentTask> tasks)
    {
        LastUnresolved.Clear();
        if (tasks == null) return;
        foreach (var t in tasks)
        {
            if (t == null || t.autoCompleteWhenOthersDone) continue;
            if (TaskTargetRegistry.Targets(t.taskId).Count == 0)
                LastUnresolved.Add(t.taskId);
        }
    }
}
