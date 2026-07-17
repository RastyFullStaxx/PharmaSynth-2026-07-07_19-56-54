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

    // ⛔ THE 2026-07-17 STUCK BUG: in play mode AddComponent fires OnEnable
    // IMMEDIATELY — before the builder can assign the vessel — so this ran with
    // vessel==null, subscribed to nothing, and SetVesselAndRunner never fixed it.
    // Result: every pour fired LiquidAdded into the void, no step ever counted,
    // and the player was stuck at "add distilled water" forever (while the
    // HazardousMixReactor — which binds correctly — kept scolding them).
    // Subscription now lives in one idempotent seam that BOTH paths call.
    private bool _subscribed;

    private void Subscribe()
    {
        if (_subscribed || vessel == null) return;
        vessel.LiquidAdded += OnLiquidAdded;
        vessel.WrongReagentMixed += OnWrongReagentMixed;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        if (vessel != null)
        {
            vessel.LiquidAdded -= OnLiquidAdded;
            vessel.WrongReagentMixed -= OnWrongReagentMixed;
        }
        _subscribed = false;
    }

    /// True once this binding actually listens to its vessel (suite-pinned: the
    /// silent-unsubscribed state is exactly the bug that shipped).
    public bool IsListening => _subscribed;

    /// Explicit unhook for teardown (ClearBenchBindings): DestroyImmediate skips
    /// OnDisable for edit-mode components whose OnEnable never ran, so relying on
    /// lifecycle left ghost subscriptions on the permanent bench vessels.
    public void Detach() => Unsubscribe();

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

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
        // A DESTROYED binding can still be subscribed (edit-mode components whose
        // OnEnable never ran get no OnDisable/OnDestroy on DestroyImmediate), and
        // its stale accumulators completed tasks on the first squeeze while its
        // stale step list scolded sanctioned pours (ghost-binding bug, found by
        // the player-path sim 2026-07-17). Unity's fake-null catches the corpse.
        if (this == null) return;
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
            if (!MetThreshold(have, step.requiredMl))
            {
                // Live pour guide (user 2026-07-17: "I can't even see how much
                // water I've poured") — a throttled running count over the vessel.
                ShowProgress(step, have);
                return;                            // keep pouring — not enough yet
            }
        }
        bool firstSatisfy = !_satisfied.Contains(step);
        _satisfied.Add(step);
        if (firstSatisfy && step.requiredMl > 0f && Application.isPlaying && vessel != null)
            FloatingText.Show("✓ " + chem.chemicalName + " — enough",
                              vessel.transform.position + Vector3.up * 0.22f,
                              new Color(0.55f, 1f, 0.65f), 0.75f);

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
    private float _nextNoteAt;   // progress-text throttle (a tilt-pour ticks every frame)

    /// "Distilled Water 6 / 10 ml" over the vessel while a metered step fills.
    private void ShowProgress(ReagentStep step, float have)
    {
        if (!Application.isPlaying || vessel == null || Time.time < _nextNoteAt) return;
        _nextNoteAt = Time.time + 0.5f;
        bool solid = step.reagent != null
                     && (step.reagent.state == PhysicalState.Solid || step.reagent.state == PhysicalState.Powder);
        FloatingText.Show(step.reagent.chemicalName + "  "
                          + Mathf.Min(have, step.requiredMl).ToString("0.#") + " / "
                          + step.requiredMl.ToString("0.#") + (solid ? " g" : " ml"),
                          vessel.transform.position + Vector3.up * 0.22f,
                          new Color(0.6f, 0.85f, 1f), 0.7f);
    }

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

    /// Threshold check with a leniency epsilon (suite-pinned): fifty 0.1 g
    /// spatula dips sum to 4.9999995 in floats — strictly-less-than left the
    /// player one phantom dip short of a 5 g step, forever (SimulatedRun,
    /// 2026-07-17). Thresholds are minimums, not calipers.
    public static bool MetThreshold(float have, float required) => have >= required - 0.01f;

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

    /// The incoming chemical is one this vessel's OWN procedure names — the
    /// wrong-mix layer (HazardousMixReactor + MixFeedback) checks this before
    /// punishing, so a sanctioned dilution ("add 10 ml of distilled water" onto
    /// the sample) never reads as "not in the procedure" again. Deliberately
    /// ignores task completion: inside one AddLiquid call LiquidAdded (which
    /// completes the task) fires BEFORE WrongReagentMixed, so a pending-only
    /// check made the COMPLETING pour punish itself (SimulatedRun caught the
    /// last sulfuric squeeze of each ester test being graded a mistake). Extra
    /// pours of a named reagent are over-pours of the right thing, not crimes.
    public bool IsExpectedNow(ChemicalData chem) => StepForReagent(chem) != null;

    // Runtime helpers for authoring/binding.
    public void AddExpected(ChemicalData reagent, string taskId, float requiredMl = 0f, bool completesTask = true)
        => expectedReagents.Add(new ReagentStep { reagent = reagent, taskId = taskId, requiredMl = requiredMl, completesTask = completesTask });

    public void SetVesselAndRunner(LiquidPhysics v, ExperimentRunner r)
    {
        Unsubscribe();          // may be re-bound to a different vessel between modules
        vessel = v; runner = r;
        Subscribe();
    }
}
