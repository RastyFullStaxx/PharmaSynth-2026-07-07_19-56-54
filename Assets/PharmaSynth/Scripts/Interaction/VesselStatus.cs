using UnityEngine;

/// Live contents readout on a vessel's existing ProximityLabel (W5.8: "track
/// the contents — texts that show when we hover or get near"). Throttled and
/// change-gated so the TMP mesh only rebuilds when the state actually moves.
public class VesselStatus : MonoBehaviour
{
    private LiquidPhysics _lp;
    private ProximityLabel _label;
    private string _displayName;
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
        string name = (_clean != null ? _clean.NamePrefix() : "") + _displayName;
        // A vessel holding a MIX names every element and its amount (the ledger
        // story) — "Ethanol 1 ml + Distilled Water 10 ml"; a single chemical
        // keeps the short form (user 2026-07-17).
        string s = _lp.Ledger.Count > 1 && !_lp.IsEmpty
            ? VesselStatusMath.ComposeMixed(name, _lp.Ledger.Summary(3))
            : VesselStatusMath.Compose(name,
                _lp.currentChemical != null ? _lp.currentChemical.chemicalName : null,
                _lp.currentLiquidVolume + _lp.currentPptVolume);
        // Zone-free heat/chill steps get a live temperature goal on the tag
        // itself (2026-07-18) — queried fresh each refresh because the builder's
        // teardown strips these components between modules.
        var heat = GetComponent<VesselHeatTask>();
        var chill = GetComponent<VesselChillTask>();
        string goal = heat != null ? VesselStatusMath.TempGoalLine(_lp.currentTempC, heat.RequiredC, false)
                    : chill != null ? VesselStatusMath.TempGoalLine(_lp.currentTempC, chill.RequiredC, true) : "";
        if (goal.Length > 0) s += "\n" + goal;
        if (s == _last) return;
        _last = s;
        _label.SetLabel(GlyphSafe.Sanitize(s), _showDist);
    }
}
