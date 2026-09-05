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

    // ---- Dynamic rack roles (W5.52) -------------------------------------------------
    //
    // A rack group's tubes are interchangeable glassware, so which ROLE a tube plays is
    // decided by what the player pours into it rather than by which bench tube it is.
    // See VesselRoleMatch for why claiming is deferred rather than decided on pour one.
    //
    // Null for a standalone vessel, which keeps the original fixed behaviour untouched.
    private List<List<ReagentStep>> _roles;
    private RackRoles _rackRoles;
    private readonly List<string> _poured = new List<string>();
    private List<IReadOnlyList<string>> _roleKeys;

    /// True while more than one of the group's roles still fits this tube. A normal
    /// state, not an error: the four alkaline tubes are indistinguishable until their
    /// third reagent lands.
    public bool RoleAmbiguous => _roles != null && _claimedRole < 0;
    public int ClaimedRole => _claimedRole;
    private int _claimedRole = -1;
    private int _authoredRole = -1;

    /// Fired ONCE, the first time this tube narrows to a single role (W5.53). The builder
    /// listens so the role's non-pour anchors (heat, chill, litmus, weigh, vapor, flame)
    /// can be attached to THIS tube — the one the player actually used — rather than to
    /// the bench tube the role was authored on. Without this, pooling the pour bindings
    /// would accept the pour and then strand the litmus step on an empty tube.
    public event System.Action<int> RoleClaimed;

    /// True for a POOL member (any tube of the family may take any role), as opposed to a
    /// rack-group member or a standalone vessel.
    public bool IsPoolMember => _roles != null && _authoredRole < 0 || _isPool;
    private bool _isPool;
    public void MarkPoolMember() => _isPool = true;

    /// Every task any SURVIVING candidate role could still serve — empty once claimed or for
    /// a fixed vessel. The tutorial sweep registers these so a free tube can be pointed at
    /// and highlighted as a valid destination before it has committed to anything.
    /// The step an ACCEPTED pour is counted against while this pool tube is still
    /// ambiguous: the first surviving candidate role's step for that reagent. Any candidate
    /// will do — the narrowing later carries the volume, by reagent, to the claimed role.
    private ReagentStep CandidateStepFor(ChemicalData chem)
    {
        if (_roles == null || _roleKeys == null || chem == null) return null;
        var candidates = VesselRoleMatch.Candidates(_roleKeys, _poured, TakenByOthers());
        foreach (int c in candidates)
            foreach (var st in _roles[c])
                if (st != null && st.reagent == chem) return st;
        return null;
    }

    public List<string> CandidateTasks()
    {
        var tasks = new List<string>();
        if (_roles == null || _claimedRole >= 0 || _roleKeys == null) return tasks;
        var candidates = VesselRoleMatch.Candidates(_roleKeys, _poured, TakenByOthers());
        foreach (int c in candidates)
            foreach (var st in _roles[c])
                if (st != null && !string.IsNullOrEmpty(st.taskId) && !tasks.Contains(st.taskId))
                    tasks.Add(st.taskId);
        return tasks;
    }

    /// Give this tube the whole group's roles. Called by ExperimentSceneBuilder once every
    /// member exists, so the roles can be shared and claimed exclusively.
    public void SetRoles(List<List<ReagentStep>> roles, int authoredRole, RackRoles shared)
    {
        _roles = roles; _rackRoles = shared; _authoredRole = authoredRole;
        _roleKeys = new List<IReadOnlyList<string>>();
        for (int i = 0; i < roles.Count; i++)
        {
            var keys = new List<string>();
            foreach (var st in roles[i])
                if (st != null && st.reagent != null && !keys.Contains(st.reagent.chemicalName))
                    keys.Add(st.reagent.chemicalName);
            _roleKeys.Add(keys);
        }
        RebuildActiveSteps();
    }

    private ICollection<int> TakenByOthers()
        => _rackRoles != null ? _rackRoles.TakenByOthers(this) : null;

    /// Recompute which steps this tube is currently working to.
    ///
    /// Claimed -> exactly that role's steps, i.e. identical to the old fixed behaviour.
    /// Ambiguous -> the union of the surviving roles, deduped by (reagent, task), so a
    /// shared prefix like "KMnO4 then NaOH" still reads and completes correctly.
    private void RebuildActiveSteps()
    {
        if (_roles == null) return;
        var candidates = VesselRoleMatch.Candidates(_roleKeys, _poured, TakenByOthers());
        int claimed = VesselRoleMatch.ClaimedRole(candidates);
        bool firstClaim = claimed >= 0 && _claimedRole < 0;
        if (claimed >= 0 && _claimedRole != claimed)
        {
            _claimedRole = claimed;
            _rackRoles?.Claim(this, claimed);
        }

        // ⛔ While AMBIGUOUS, show the tube's AUTHORED role - never the union of the
        // survivors. Exposing the union made every enol tube advertise all five alcohols,
        // and anything that reads ExpectedSteps to decide where a reagent goes (the
        // player-path simulator, the vessel label, the rack membership) then treated every
        // tube as a valid target: the sim poured all five alcohols into all five tubes and
        // logged 144 mistakes on a PERFECT run. Acceptance stays permissive - that is the
        // whole point of dynamic roles - but the ADVERTISED step must stay a single
        // deterministic answer.
        int show = claimed >= 0 ? claimed : _authoredRole;
        var next = new List<ReagentStep>();
        if (show >= 0 && show < _roles.Count) next.AddRange(_roles[show]);

        // Carry accumulated volume across the narrowing. The same reagent exists as a
        // DIFFERENT ReagentStep object in each role, so without this remap the water a
        // player poured while the tube was still ambiguous would vanish the moment an
        // alcohol claimed a different role, and they would be asked to pour it again.
        //
        // ⛔ Match by REAGENT, preferring the same task but never requiring it. Two roles
        // routinely expect the same liquid under DIFFERENT task ids (Exp 6: acetone for
        // `test-tollens` in one role, `test-schiff` in another), and 2 ml of acetone in the
        // tube is 2 ml of acetone whatever the claimed role ends up calling the step.
        var carriedAmount = new Dictionary<ReagentStep, float>();
        var carriedDone = new HashSet<ReagentStep>();
        foreach (var st in next)
        {
            ReagentStep bestAmt = null;
            foreach (var kv in _accumulated)
            {
                if (kv.Key.reagent != st.reagent) continue;
                if (bestAmt == null || kv.Key.taskId == st.taskId) bestAmt = kv.Key;
                if (kv.Key.taskId == st.taskId) break;
            }
            if (bestAmt != null) carriedAmount[st] = _accumulated[bestAmt];
            foreach (var done in _satisfied)
                if (done.reagent == st.reagent && (done.taskId == st.taskId || !carriedDone.Contains(st)))
                { if (done.taskId == st.taskId || done.requiredMl >= st.requiredMl) carriedDone.Add(st); if (done.taskId == st.taskId) break; }
        }
        _accumulated.Clear();
        foreach (var kv in carriedAmount) _accumulated[kv.Key] = kv.Value;
        _satisfied.Clear();
        foreach (var st in carriedDone) _satisfied.Add(st);

        expectedReagents.Clear();
        expectedReagents.AddRange(next);

        // Raised AFTER the active steps are rebuilt, so a listener attaching the role's
        // anchors sees ExpectedSteps / ReadyFor already describing the claimed role.
        if (firstClaim) RoleClaimed?.Invoke(claimed);
    }

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

    /// Builder seam: the lab's fume hood (assigned per build — Exp 5's aniline
    /// and acetyl chloride pours are only sanctioned inside it).
    public void SetFumeHood(FumeHoodZone hood) => fumeHood = hood;

    /// Whether this vessel currently counts as "in the fume hood".
    public bool InFumeHood()
        => fumeHood != null && (fumeHood.IsOccupied || fumeHood.Contains(transform.position));

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

        // Fume-hood safety: a toxic/volatile reagent handled outside the hood is
        // a violation. "In the hood" = THIS VESSEL sits inside the hood volume
        // (position test — the work happens where the vessel is; 2026-07-18, the
        // old hand-occupancy trigger was never wired and always violated) — or
        // the physics occupancy when the trigger setup exists.
        if (chem.requiresFumeHood && !InFumeHood())
            runner.RecordMistake(LabErrorType.FumeHoodViolation, chem.chemicalName + " must be handled in the fume hood");

        // A RACK TUBE grades the pour against its surviving roles, not against a fixed
        // list: pouring the right reagent into the "wrong" numbered tube is correct play
        // in VR, where the player grabs whichever tube is nearest. Only a reagent that no
        // surviving role wants is a real mistake (user 2026-09-05, in the headset).
        if (_roles != null)
        {
            if (!VesselRoleMatch.WouldAccept(_roleKeys, _poured, chem.chemicalName, TakenByOthers()))
            {
                runner.RecordMistake(LabErrorType.WrongReagent, "Unexpected reagent: " + chem.chemicalName);
                return;
            }
            if (!_poured.Contains(chem.chemicalName)) _poured.Add(chem.chemicalName);
            RebuildActiveSteps();
        }

        var step = StepForReagent(chem);
        // ⛔ An AMBIGUOUS pool extra advertises no steps at all (so the simulator never
        // targets it), yet the candidate set just ACCEPTED this pour. Without this branch the
        // legacy lookup below found nothing and recorded "unexpected reagent" on a pour the
        // tube had said yes to — the first pour into any free tube was scolded and only the
        // second landed (found on the real Exp 6 / Exp 8 stages, 2026-09-06). Count it against
        // the first surviving candidate's step for this reagent; the narrowing carries the
        // volume to whichever role the tube finally claims.
        if (step == null && _roles != null && _claimedRole < 0) step = CandidateStepFor(chem);
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
