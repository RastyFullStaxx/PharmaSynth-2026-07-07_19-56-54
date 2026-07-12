using UnityEngine;

/// The STIR verb (W5.8): circle the glass rod inside this vessel's mouth while
/// it holds liquid and the stir task completes — works with the vessel on the
/// table OR held in a hand (everything is vessel-relative). Progress pops as
/// floating text; the TaskGraph condition owns completion (Retry-safe via the
/// ZoneSimStation resubscribe pattern).
public class StirController : MonoBehaviour
{
    private readonly OrbitMath _math = new OrbitMath();
    private ExperimentRunner _runner;
    private string _taskId;
    private LiquidPhysics _lp;
    private Transform _rod;
    private float _mouthRadius = 0.09f;
    private float _rimBandY = 0.30f;
    private bool _subscribed;
    private bool _doneAnnounced;
    private int _lastShownPct = -1;
    private float _nextPopupAt;

    public OrbitMath Math => _math;

    /// Builder seam.
    public void Bind(ExperimentRunner runner, string taskId, LiquidPhysics lp, Transform rod,
                     float mouthRadius = 0.09f, float rimBandY = 0.30f, float requiredRevs = 2.5f)
    {
        _runner = runner; _taskId = taskId; _lp = lp; _rod = rod;
        _mouthRadius = mouthRadius; _rimBandY = rimBandY;
        _math.requiredRevs = requiredRevs;
        Resubscribe();
        Register();
    }

    /// The rod prop can respawn — rebindable.
    public void SetRod(Transform rod) => _rod = rod;

    // W5.9: also re-Register on enable — a station toggled off/on mid-run used
    // to lose its condition on the current graph until the next start.
    private void OnEnable() { Resubscribe(); Register(); }
    private void OnDisable() { if (_runner != null) _runner.ExperimentStarted -= OnStarted; _subscribed = false; }

    private void Resubscribe()
    {
        if (_runner == null || _subscribed) return;
        _runner.ExperimentStarted += OnStarted;
        _subscribed = true;
    }

    private void OnStarted(ExperimentModuleDefinition _) => Register();

    public void Register()
    {
        if (_runner == null || _runner.Graph == null || string.IsNullOrEmpty(_taskId)) return;
        _math.Reset();
        _doneAnnounced = false;
        _lastShownPct = -1;
        _runner.Graph.RegisterCondition(_taskId, () => _math.IsDone);
    }

    /// True while the rod tip is inside the mouth band (vessel-relative).
    public bool RodInMouth()
    {
        if (_rod == null) return false;
        Vector3 d = _rod.position - transform.position;
        float horiz = new Vector2(d.x, d.z).magnitude;
        return horiz <= _mouthRadius && d.y > -0.02f && d.y <= _rimBandY;
    }

    private void Update()
    {
        if (_runner == null || !_runner.IsRunning || _rod == null || _lp == null) return;
        if (_lp.currentLiquidVolume <= 1f) return;   // nothing to stir yet

        Vector3 d = _rod.position - transform.position;
        Tick(d.x, d.z, RodInMouth());
    }

    /// One stir sample (public so tests can drive it without physics).
    public void Tick(float x, float z, bool inside)
    {
        bool wasDone = _math.IsDone;
        _math.Feed(x, z, inside);

        if (!inside) return;
        int pct = Mathf.RoundToInt(_math.Progress01 * 100f);
        if (!_math.IsDone && pct != _lastShownPct && pct % 25 == 0 && pct > 0 && Time.time >= _nextPopupAt)
        {
            _lastShownPct = pct;
            _nextPopupAt = Time.time + 0.8f;
            FloatingText.Show("Stirring... " + pct + "%", transform.position + Vector3.up * 0.25f, new Color(0.7f, 0.9f, 1f), 0.8f);
            AudioService.TryPlayAt("stir", transform.position);
        }
        if (_math.IsDone && !wasDone && !_doneAnnounced)
        {
            _doneAnnounced = true;
            FloatingText.Show("Well stirred!", transform.position + Vector3.up * 0.28f, new Color(0.6f, 1f, 0.7f));
            AudioService.TryPlayAt("mixture-complete", transform.position);
        }
    }
}
