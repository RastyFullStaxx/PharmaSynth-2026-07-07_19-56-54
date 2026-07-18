using UnityEngine;

/// Pure rules for the ZONE-FREE ice bath (Exp 4 crystallisation, Exp 8's ice
/// bath) — the cold twin of WaterBathMath. The bucket needs nothing lit or
/// poured: it IS ice. Set any vessel in (or beside) it, anywhere in the lab,
/// and the vessel takes ice-water temperature.
public static class IceBathMath
{
    /// Vessels within this distance of the bucket take its temperature
    /// (same reach as the water bath — one spatial contract for both tools).
    public const float VesselRadius = 0.32f;
    /// Ice-water sits a couple of degrees above zero.
    public const float IceWaterC = 2f;

    /// A vessel "in the ice" is simply near enough. Pure so the suite pins it.
    public static bool Chills(float distance) => distance <= VesselRadius;

    public static string StatusLine() => "Ice Bath — set a vessel in it to chill";
}

/// The bucket itself: every LiquidPhysics vessel brought close is pulled to
/// ice-water temperature. No station, no pad — carry the bucket or carry the
/// flask, either way works (the zone-free tool rule, user 2026-07-17).
public class IceBathController : MonoBehaviour
{
    [SerializeField] private ProximityLabel _label;
    private float _nextScan;

    /// Edit-mode / wiring seam.
    public void Bind(ProximityLabel label) => _label = label;

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.25f;

        foreach (var col in Physics.OverlapSphere(transform.position, IceBathMath.VesselRadius,
                                                  ~0, QueryTriggerInteraction.Ignore))
        {
            var lp = col != null ? col.GetComponentInParent<LiquidPhysics>() : null;
            if (lp != null && !lp.transform.IsChildOf(transform)) ChillVessel(lp);
        }
        if (_label == null) _label = GetComponentInChildren<ProximityLabel>(true);
        if (_label != null) _label.SetLabel(IceBathMath.StatusLine(), 1.4f);
    }

    /// Explicitly chill one vessel (SimulatedRun's stand-in for physically
    /// setting the flask in the ice — mirrors WaterBathController.HeatVessel).
    public void ChillVessel(LiquidPhysics vessel)
    { if (vessel != null) vessel.SetTemperature(IceBathMath.IceWaterC); }
}

/// Completes a chill step ZONE-FREE: the task is done when this vessel actually
/// HOLDS something and has been brought down to the required temperature —
/// however and wherever the player cooled it. The cold twin of VesselHeatTask
/// (Exp 4's "cool in an ice bath; crystallise"). Ambient is 25 °C, so a vessel
/// can never satisfy a chill threshold by simply standing on the bench.
public class VesselChillTask : MonoBehaviour
{
    private ExperimentRunner _runner;
    private string _taskId;
    private float _requiredC;
    private LiquidPhysics _lp;
    private bool _subscribed;

    public string TaskId => _taskId;
    public float RequiredC => _requiredC;

    /// Pure (suite-pinned): holding something AND cold — never one without the other.
    public static bool ShouldComplete(bool hasContents, float tempC, float requiredC)
        => hasContents && tempC <= requiredC;

    public void Bind(ExperimentRunner runner, string taskId, float requiredC, LiquidPhysics lp)
    {
        if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; }
        _runner = runner; _taskId = taskId; _requiredC = requiredC; _lp = lp;
        if (_runner != null) { _runner.ExperimentStarted += OnStarted; _subscribed = true; }
        Register();
    }

    /// Teardown seam (ClearBenchBindings) — bench items survive rebuilds.
    public void Detach()
    { if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; } }

    private void OnDestroy() => Detach();
    private void OnStarted(ExperimentModuleDefinition _) => Register();

    private void Register()
    {
        if (_runner == null || _runner.Graph == null || string.IsNullOrEmpty(_taskId)) return;
        _runner.Graph.RegisterCondition(_taskId, () =>
            _lp != null
            && ShouldComplete(_lp.currentLiquidVolume + _lp.currentPptVolume > 0.5f,
                              _lp.currentTempC, _requiredC));
    }
}
