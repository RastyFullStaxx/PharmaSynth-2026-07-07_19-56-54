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

    /// Builder seam (Awake doesn't fire on edit-mode AddComponent).
    public void Bind(LiquidPhysics lp, ProximityLabel label, string displayName, float showDist = 1.6f)
    {
        _lp = lp; _label = label; _displayName = displayName; _showDist = showDist;
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
        string s = VesselStatusMath.Compose(_displayName,
            _lp.currentChemical != null ? _lp.currentChemical.chemicalName : null,
            _lp.currentLiquidVolume + _lp.currentPptVolume);
        if (s == _last) return;
        _last = s;
        _label.SetLabel(GlyphSafe.Sanitize(s), _showDist);
    }
}
