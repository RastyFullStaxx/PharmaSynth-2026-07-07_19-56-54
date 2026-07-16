using System.Collections.Generic;
using UnityEngine;

/// Bridges a vessel's LiquidPhysics chemistry events to the experiment logic in a
/// context-aware way: adding a reagent completes the task that expects it (the
/// TaskGraph's prerequisite check enforces order), while a reagent no step expects
/// is a genuine wrong-reagent mistake. Steps may require a MINIMUM poured amount
/// (requiredMl) — deliveries accumulate until the threshold is met, so a one-frame
/// splash no longer completes a step (client depletion mechanic, 2026-07-09).
public class LiquidTaskBinding : MonoBehaviour
{
    [System.Serializable]
    public class ReagentStep
    {
        public ChemicalData reagent;
        public string taskId;
        [Tooltip("Minimum ml poured in before the step completes. 0 = any amount (legacy).")]
        public float requiredMl;
        [Tooltip("False = the pour is EXPECTED (no wrong-reagent mistake) and accumulates, but completion belongs to another verb (e.g. the weigh station). (W5.8)")]
        public bool completesTask = true;
    }

    [SerializeField] private LiquidPhysics vessel;
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private List<ReagentStep> expectedReagents = new List<ReagentStep>();
    [SerializeField] private FumeHoodZone fumeHood;   // toxic reagents must be handled here

    // Accumulate per STEP, not per task: a task may name SEVERAL reagents (the
    // iodoform test needs KI *and* hypochlorite; Exp 2's tube prep needs the
    // sample *and* its water), and a task-keyed total pooled them — so whichever
    // reagent landed first met the threshold alone and completed the step with
    // half the chemistry missing. Steps are satisfied individually and the task
    // waits for all of them (2026-07-16).
    private readonly Dictionary<ReagentStep, float> _accumulated = new Dictionary<ReagentStep, float>();
    private readonly HashSet<ReagentStep> _satisfied = new HashSet<ReagentStep>();

    public IReadOnlyList<ReagentStep> ExpectedSteps => expectedReagents;

    private void OnEnable()
    {
        if (vessel != null)
        {
            vessel.LiquidAdded += OnLiquidAdded;
            vessel.WrongReagentMixed += OnWrongReagentMixed;
        }
    }

    private void OnDisable()
    {
        if (vessel != null)
        {
            vessel.LiquidAdded -= OnLiquidAdded;
            vessel.WrongReagentMixed -= OnWrongReagentMixed;
        }
    }

    private void OnLiquidAdded(ChemicalData chem, float amount) => HandleReagent(chem, amount);

    private void OnWrongReagentMixed(ChemicalData current, ChemicalData incoming)
    {
        // Already handled by HandleReagent via LiquidAdded; nothing extra needed here.
    }

    /// Legacy single-arg path (self-tests, scripted deliveries): treated as a FULL
    /// delivery — the step completes regardless of its requiredMl threshold.
    public void HandleReagent(ChemicalData chem) => Handle(chem, 0f, true);

    /// Amount-aware handling: pours accumulate toward the step's requiredMl.
    public void HandleReagent(ChemicalData chem, float amountMl) => Handle(chem, amountMl, false);

    private void Handle(ChemicalData chem, float amountMl, bool fullDelivery)
    {
        if (runner == null || chem == null) return;

        // Fume-hood safety: a toxic/volatile reagent handled outside the hood is a violation.
        if (chem.requiresFumeHood && (fumeHood == null || !fumeHood.IsOccupied))
            runner.RecordMistake(LabErrorType.FumeHoodViolation, chem.chemicalName + " must be handled in the fume hood");

        var step = StepForReagent(chem);
        if (step == null)
        {
            // No step in this experiment expects this reagent → wrong reagent.
            runner.RecordMistake(LabErrorType.WrongReagent, "Unexpected reagent: " + chem.chemicalName);
            return;
        }

        // Already done? Ignore extra pours of the same reagent (no double-completes).
        if (runner.Graph != null && runner.Graph.IsComplete(step.taskId)) return;

        if (!fullDelivery && step.requiredMl > 0f)
        {
            _accumulated.TryGetValue(step, out float have);
            have += Mathf.Max(0f, amountMl);
            _accumulated[step] = have;
            if (have < step.requiredMl) return;    // keep pouring — not enough yet
        }
        _satisfied.Add(step);

        // A task that names several reagents in this vessel needs them ALL before
        // it can be called done — half a recipe is not the step.
        if (!AllStepsSatisfied(step.taskId)) return;

        // Every step satisfied. Tasks owned by another verb (W5.8: the weigh
        // station; 2026-07-16: a RackTaskGroup that waits for every tube) only
        // flag readiness — no wrong-reagent mistake was recorded (the pour IS
        // expected), but completion is theirs.
        if (!CompletesHere(step.taskId)) { _ready.Add(step.taskId); return; }

        // Enough reagent delivered. CompleteTask enforces order and will
        // auto-record a WrongStep mistake if prerequisites aren't met yet.
        runner.CompleteTask(step.taskId);
    }

    private readonly HashSet<string> _ready = new HashSet<string>();

    /// Every reagent this task names in THIS vessel has met its own threshold.
    private bool AllStepsSatisfied(string taskId)
    {
        bool any = false;
        for (int i = 0; i < expectedReagents.Count; i++)
        {
            var s = expectedReagents[i];
            if (s == null || s.taskId != taskId) continue;
            any = true;
            if (!_satisfied.Contains(s)) return false;
        }
        return any;
    }

    /// False when ANY of the task's steps defers completion to another verb.
    private bool CompletesHere(string taskId)
    {
        for (int i = 0; i < expectedReagents.Count; i++)
        {
            var s = expectedReagents[i];
            if (s != null && s.taskId == taskId && !s.completesTask) return false;
        }
        return true;
    }

    /// This vessel has everything the task asked of it (the rack group's poll).
    public bool ReadyFor(string taskId) => _ready.Contains(taskId);

    /// Delivered-so-far toward a step — SUMMED across the task's reagents, which
    /// is what the supply monitor wants (how much has gone in for this step).
    public float AccumulatedFor(string taskId)
    {
        float total = 0f;
        foreach (var kv in _accumulated)
            if (kv.Key != null && kv.Key.taskId == taskId) total += kv.Value;
        return total;
    }

    /// Delivered-so-far of ONE reagent toward a step.
    public float AccumulatedFor(string taskId, ChemicalData reagent)
    {
        foreach (var kv in _accumulated)
            if (kv.Key != null && kv.Key.taskId == taskId && kv.Key.reagent == reagent) return kv.Value;
        return 0f;
    }

    /// Reagents this task still expects in this vessel (watch-panel / debug).
    public int StepsRemaining(string taskId)
    {
        int n = 0;
        for (int i = 0; i < expectedReagents.Count; i++)
        {
            var s = expectedReagents[i];
            if (s != null && s.taskId == taskId && !_satisfied.Contains(s)) n++;
        }
        return n;
    }

    public ReagentStep StepForReagent(ChemicalData chem)
    {
        for (int i = 0; i < expectedReagents.Count; i++)
            if (expectedReagents[i] != null && expectedReagents[i].reagent == chem)
                return expectedReagents[i];
        return null;
    }

    public string TaskForReagent(ChemicalData chem)
    {
        var s = StepForReagent(chem);
        return s != null ? s.taskId : null;
    }

    // Runtime helpers for authoring/binding.
    public void AddExpected(ChemicalData reagent, string taskId, float requiredMl = 0f, bool completesTask = true)
        => expectedReagents.Add(new ReagentStep { reagent = reagent, taskId = taskId, requiredMl = requiredMl, completesTask = completesTask });

    public void SetVesselAndRunner(LiquidPhysics v, ExperimentRunner r) { vessel = v; runner = r; }
}
