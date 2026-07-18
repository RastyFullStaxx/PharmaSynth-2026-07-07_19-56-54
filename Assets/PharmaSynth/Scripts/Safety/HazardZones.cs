using UnityEngine;

/// A fume-hood working volume. Tracks whether the player's hand / active work is
/// inside it, so toxic/volatile reagents can require the hood (plan §3.7).
public class FumeHoodZone : MonoBehaviour
{
    [SerializeField] private string occupantTag = "";   // e.g. player hand collider tag
    private int _occupants;

    public bool IsOccupied => _occupants > 0;

    /// Position-based test (used by reagent validation without needing physics).
    public bool Contains(Vector3 worldPos)
    {
        var col = GetComponent<Collider>();
        return col != null && col.bounds.Contains(worldPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(occupantTag) || other.CompareTag(occupantTag)) _occupants++;
    }
    private void OnTriggerExit(Collider other)
    {
        if (string.IsNullOrEmpty(occupantTag) || other.CompareTag(occupantTag)) _occupants = Mathf.Max(0, _occupants - 1);
    }
}

/// Narrating label for the fume hood (2026-07-18, user: "how do we use the
/// fumehood? does it open?"): the hood is an OPEN alcove — nothing to open or
/// switch on; protection is purely WHERE the vessel is. The invisible
/// WorkVolume made "am I in far enough?" a guess, so the label says what
/// belongs inside and flips to a ✓ while a vessel is actually protected.
public class FumeHoodStatusLabel : MonoBehaviour
{
    private FumeHoodZone _zone;
    private ProximityLabel _label;
    private float _nextScan;

    public void Bind(FumeHoodZone zone, ProximityLabel label) { _zone = zone; _label = label; }

    /// Pure (suite-pinned): idle guidance vs the in-hood assurance cue.
    public static string StatusLine(string vesselInside)
        => string.IsNullOrEmpty(vesselInside)
            ? "Fume Hood — do aniline & acetyl chloride work IN here"
            : "Fume Hood — " + vesselInside + " protected ✓";

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.25f;
        if (_zone == null) _zone = GetComponent<FumeHoodZone>() ?? GetComponentInParent<FumeHoodZone>();
        if (_label == null) _label = GetComponent<ProximityLabel>() ?? gameObject.AddComponent<ProximityLabel>();
        if (_zone == null || _label == null) return;
        string inside = null;
        foreach (var lp in FindObjectsByType<LiquidPhysics>(FindObjectsSortMode.None))
            if (lp != null && _zone.Contains(lp.transform.position))
            { inside = Mishandling.DisplayNameFor(lp.gameObject); break; }
        _label.SetLabel(StatusLine(inside), 2.2f);
    }
}

/// A hazard volume (spill, hot surface, corrosive) — contact reports a mistake to
/// the runner and can trigger a visual/audio warning. Debounced so a dwell reports once.
public class HazardZone : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private LabErrorType errorType = LabErrorType.ChemicalContact;
    [SerializeField] private string message = "Chemical contact!";
    [SerializeField] private string contactTag = "";   // player/hand
    [SerializeField] private float rearmSeconds = 2f;

    private float _lastReport = -999f;
    private Transform _playerRoot;
    private System.Func<bool> _armed;

    public void SetRunner(ExperimentRunner r) => runner = r;

    /// Builder seam: hazard identity in one call.
    public void Configure(ExperimentRunner r, LabErrorType type, string msg)
    { runner = r; errorType = type; message = msg; }

    /// When set, only colliders under the rig root count (props placed into the
    /// zone never trigger it) — same player test PlayerTriggerRelay uses.
    public void SetPlayerRoot(Transform root) => _playerRoot = root;

    /// When set, contact only reports while armed (e.g. hot surface only counts
    /// once the temperature sim is actually hot).
    public void SetArmedCheck(System.Func<bool> armed) => _armed = armed;

    /// Pure player-membership test (self-tests pin it).
    public static bool IsPlayer(Transform other, Transform playerRoot)
        => playerRoot != null && other != null && (other == playerRoot || other.IsChildOf(playerRoot));

    private void OnTriggerEnter(Collider other)
    {
        if (_playerRoot != null && !IsPlayer(other.transform, _playerRoot)) return;
        if (!string.IsNullOrEmpty(contactTag) && !other.CompareTag(contactTag)) return;
        Report();
    }

    /// Public so it is directly testable / callable by non-physics detectors.
    public void Report()
    {
        if (runner == null) return;
        if (_armed != null && !_armed()) return;
        if (Time.time - _lastReport < rearmSeconds) return;
        _lastReport = Time.time;
        runner.RecordMistake(errorType, message);
    }
}
