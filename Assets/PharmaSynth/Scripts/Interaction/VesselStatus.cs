using UnityEngine;

/// Live contents readout on a vessel's existing ProximityLabel (W5.8: "track
/// the contents — texts that show when we hover or get near"). Throttled and
/// change-gated so the TMP mesh only rebuilds when the state actually moves.
public class VesselStatus : MonoBehaviour
{
    private LiquidPhysics _lp;
    private ProximityLabel _label;
    private string _displayName;

    /// " — Tollen's test" once a pooled tube has claimed a role (W5.53), so the tube the
    /// player chose says what it has become. Derived from the claimed role's task LABEL —
    /// never the layout's internal role name, which the 2026-07-17 rule keeps off the bench.
    private string _roleSuffix = "";
    public void SetRoleSuffix(string suffix) => _roleSuffix = string.IsNullOrEmpty(suffix) ? "" : " — " + suffix;
    private float _showDist = 1.6f;
    private float _nextAt;
    private string _last;
    private CleanableVessel _clean;   // optional: "Dirty "/"Clean " name prefix (W5.12)

    /// Builder seam (Awake doesn't fire on edit-mode AddComponent).
    public void Bind(LiquidPhysics lp, ProximityLabel label, string displayName, float showDist = 1.6f)
    {
        _lp = lp; _label = label; _displayName = displayName; _showDist = showDist;
        _clean = GetComponent<CleanableVessel>();
        Refresh();
    }

    /// Every ref here is PRIVATE and unserialized, so a hand-placed bench vessel
    /// that nobody calls Bind() on stayed mute forever — the contents text simply
    /// "wasn't showing at all in some test tubes" (user 2026-07-27). Adopt the
    /// vessel's own components at Awake; a later Bind() still wins.
    private void Awake()
    {
        if (_lp != null) return;
        var lp = GetComponent<LiquidPhysics>();
        var label = GetComponent<ProximityLabel>();
        if (lp == null || label == null) return;
        var item = GetComponent<LabItem>();
        string name = item != null && !string.IsNullOrEmpty(item.displayName)
            ? item.displayName : Mishandling.DisplayNameFor(gameObject);
        Bind(lp, label, name, _showDist);
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextAt) return;
        _nextAt = Time.unscaledTime + 0.25f;
        Refresh();
    }

    /// Public for tests + immediate updates.
    public void Refresh()
    {
        if (_lp == null || _label == null) return;
        if (_clean == null) _clean = GetComponent<CleanableVessel>();
        string name = (_clean != null ? _clean.NamePrefix() : "") + _displayName + _roleSuffix;
        // A vessel holding a MIX names every element and its amount (the ledger
        // story) — "Ethanol 1 ml + Distilled Water 10 ml"; a single chemical
        // keeps the short form (user 2026-07-17).
        string s = _lp.Ledger.Count > 1 && !_lp.IsEmpty
            ? VesselStatusMath.ComposeMixed(name, _lp.Ledger.Summary(VesselLedger.All))
            : VesselStatusMath.Compose(name,
                _lp.currentChemical != null ? _lp.currentChemical.chemicalName : null,
                _lp.currentLiquidVolume + _lp.currentPptVolume);
        // Zone-free heat/chill steps get a live temperature goal on the tag
        // itself (2026-07-18) — queried fresh each refresh because the builder's
        // teardown strips these components between modules. Only while the
        // owning step is the player's CURRENT concern (Relevant): Exp 5's flask
        // used to read "chill to 8 C" from step 1.
        var heat = GetComponent<VesselHeatTask>();
        var chill = GetComponent<VesselChillTask>();
        string goal = heat != null && heat.Relevant
                        ? VesselStatusMath.TempGoalLine(_lp.currentTempC, heat.RequiredC, false)
                    : chill != null && chill.Relevant
                        ? VesselStatusMath.TempGoalLine(_lp.currentTempC, chill.RequiredC, true) : "";
        if (goal.Length > 0) s += "\n" + goal;
        // What this vessel still wants, and why a pour bounced (W5.44, widened W5.55 on the
        // user's ask: "give a hint to what is still needed or what is wrong ... dynamically
        // to any apparatus as players can use anything"). Queried fresh each refresh for the
        // same reason the heat/chill goals are — the builder's teardown strips the binding
        // between modules. Shown only on the glass actually in use (DetailWanted), or the
        // bench becomes a wall of numbers at arm's length.
        if (DetailWanted())
        {
            string need = NeedLineNow();
            if (need.Length > 0) s += NewLine + need;
            var b = GetComponent<LiquidTaskBinding>();
            if (b != null)
            {
                string refused = VesselStatusMath.RefusalLine(b.RefusedReagent);
                if (refused.Length > 0) s += NewLine + refused;
                string free = VesselStatusMath.FreeVesselLine(b.IsPoolMember, b.RoleAmbiguous, _lp.IsEmpty);
                if (free.Length > 0) s += NewLine + free;
            }
        }

        if (s == _last) return;
        _last = s;
        _label.SetLabel(GlyphSafe.Sanitize(s), _showDist);
    }

    const string NewLine = "\n";

    /// How square-on the player has to be looking for a vessel to count as "the one I am
    /// reading". Tight, because two tubes in a rack are only degrees apart.
    private const float GazeDot = 0.985f;

    /// Is this the glass the player is actually using? Held, being looked at from within
    /// label range, or bound to the step being guided. Anything else keeps the short
    /// contents line — the W5.52 lesson was that a bench of vessels all advertising numbers
    /// is what sent the player to the wrong tube in the first place.
    private bool DetailWanted()
    {
        if (TutorialHighlighter.IsHeld(transform)) return true;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 to = transform.position - cam.transform.position;
            float d = to.magnitude;
            if (d > 0.01f && d <= _showDist
                && Vector3.Dot(cam.transform.forward, to / d) >= GazeDot) return true;
        }
        return GuidedStepWantsThis();
    }

    /// True when this vessel advertises a step of the task currently being guided.
    private bool GuidedStepWantsThis()
    {
        var binding = GetComponent<LiquidTaskBinding>();
        if (binding == null || binding.ExpectedSteps == null) return false;
        string guided = GuidedTask();
        if (string.IsNullOrEmpty(guided)) return false;
        foreach (var step in binding.ExpectedSteps)
            if (step != null && step.taskId == guided) return true;
        return false;
    }

    /// The FIRST available task — GuidePath's own convention for "the step being guided".
    private static string GuidedTask()
    {
        var runner = FindAnyObjectByType<ExperimentRunner>();
        if (runner == null || runner.Graph == null || !runner.IsRunning) return null;
        foreach (var t in runner.Graph.AvailableTasks()) return t.taskId;
        return null;
    }

    /// The guided step's outstanding amount for THIS vessel, or "" when it is not the
    /// vessel currently being asked for. Only the guided step is shown: every bound vessel
    /// printing its whole shopping list would turn the bench into a wall of numbers.
    private string NeedLineNow()
    {
        var binding = GetComponent<LiquidTaskBinding>();
        if (binding == null || binding.ExpectedSteps == null) return "";
        var runner = FindAnyObjectByType<ExperimentRunner>();
        if (runner == null || runner.Graph == null || !runner.IsRunning) return "";

        // ⛔ The GUIDED task only - not every AVAILABLE one. This used to walk the whole
        // AvailableTasks() list, so with Exp 2's prep steps running in parallel the four
        // alkaline tubes advertised "Potassium Permanganate 0 / 2 ml" while the player was
        // still on the ethanol step, and one of them read as the tube being asked for. That
        // is what sent the player to the wrong tube in the headset (2026-09-05). GuidePath
        // already treats the FIRST available task as the guided one; reuse that rather than
        // inventing a second notion of "the current step".
        string guided = GuidedTask();
        if (string.IsNullOrEmpty(guided)) return "";

        foreach (var step in binding.ExpectedSteps)
        {
            if (step == null || step.reagent == null || step.taskId != guided) continue;
            if (step.requiredMl <= 0f) continue;
            return VesselStatusMath.NeedLine(step.reagent.chemicalName,
                binding.AccumulatedFor(step.taskId, step.reagent), step.requiredMl,
                step.reagent.state == PhysicalState.Solid);
        }
        return "";
    }
}
