using UnityEngine;

/// Pure rules for NAKED-FLAME heating (Exp 6's dry distillation — the ONE step
/// in any experiment that heats over an open flame; everything else is a
/// ≤100 °C water bath). A vessel held over a LIT burner climbs toward a
/// red-glow temperature the bath can never reach.
public static class FlameMath
{
    /// A vessel this close to the burner's flame takes its heat.
    public const float Reach = 0.18f;
    /// An open flame tops out far beyond the bath's 100 °C cap.
    public const float MaxC = 400f;
    /// Degrees per second while over the flame.
    public const float RatePerSecond = 60f;

    public static bool Heats(float distance) => distance <= Reach;

    public static float NextTemp(float current, float dt)
        => Mathf.Min(MaxC, current + RatePerSecond * Mathf.Max(0f, dt));
}

/// Zone-free open-flame heat on every Bunsen burner (wired by Apply W5.8):
/// while LIT, any vessel within reach of the flame anchor heats toward 400 °C.
/// This is what the hard-glass tubes exist for — the water bath owns every
/// gentler step.
public class NakedFlameHeat : MonoBehaviour
{
    private BurnerController _burner;
    private Transform _flame;
    private float _nextScan;

    public void Bind(BurnerController burner) { _burner = burner; _flame = null; }

    private Vector3 FlamePos()
    {
        if (_flame == null) _flame = transform.Find("FlameAnchor");
        return _flame != null ? _flame.position : transform.position + Vector3.up * 0.12f;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.25f;
        if (_burner == null) _burner = GetComponent<BurnerController>();
        if (_burner == null || !_burner.IsLit) return;
        foreach (var col in Physics.OverlapSphere(FlamePos(), FlameMath.Reach, ~0, QueryTriggerInteraction.Ignore))
        {
            var lp = col != null ? col.GetComponentInParent<LiquidPhysics>() : null;
            if (lp != null && !lp.transform.IsChildOf(transform)) HeatVessel(lp, 0.25f);
        }
    }

    /// One heating step for a vessel over the flame (SimulatedRun's stand-in
    /// for physically holding the tube there — mirrors WaterBath.HeatVessel).
    public void HeatVessel(LiquidPhysics vessel, float dt)
    { if (vessel != null) vessel.SetTemperature(FlameMath.NextTemp(vessel.currentTempC, dt)); }
}

/// Pure rules for VAPOR COLLECTION (Exp 6: distill the acetone off the glowing
/// acetates at 56 °C into a receiver tube).
public static class VaporMath
{
    /// The receiver must sit this close to the hot tube's mouth (the delivery
    /// tube bridges them, like Exp 3's CO₂ run).
    public const float DeliveryRadius = 0.5f;
    /// Millilitres condensed per emission tick.
    public const float MlPerTick = 0.5f;

    /// Vapor comes off only while the source is at temperature AND still has
    /// charge left to decompose.
    public static bool Fires(float sourceTempC, float requiredC, float sourceMl)
        => sourceTempC >= requiredC && sourceMl > 0.5f;
}

/// The dry-distillation product stream, zone-free (the fermentation pattern):
/// once the collect task is AVAILABLE and the source tube is at temperature,
/// it converts its charge into the module's product, condensing into the
/// nearest nearby vessel whose binding EXPECTS the product — targeted, so the
/// stream can never pollute the water bath or a bystander tube.
public class VaporCollectController : MonoBehaviour
{
    private ExperimentRunner _runner;
    private LiquidPhysics _source;
    private string _taskId;
    private ChemicalData _product;
    private float _requiredC;
    private float _nextTick;

    public string VaporTaskId => _taskId;
    public LiquidPhysics Source => _source;

    public void Bind(ExperimentRunner runner, LiquidPhysics source, string taskId,
                     ChemicalData product, float requiredC)
    { _runner = runner; _source = source; _taskId = taskId; _product = product; _requiredC = requiredC; }

    /// Teardown seam (ClearBenchBindings).
    public void Detach() { _runner = null; }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + 0.4f;
        if (_runner == null || _runner.Graph == null || !_runner.IsRunning) return;
        if (!_runner.Graph.IsAvailable(_taskId)) return;   // not this step yet
        if (_source == null || _product == null) return;
        if (!VaporMath.Fires(_source.currentTempC, _requiredC, _source.currentLiquidVolume)) return;

        LiquidPhysics receiver = null; float best = float.MaxValue;
        foreach (var b in FindObjectsByType<LiquidTaskBinding>(FindObjectsSortMode.None))
        {
            var lp = b.GetComponent<LiquidPhysics>();
            if (lp == null || lp == _source || !b.IsExpectedNow(_product)) continue;
            float d = Vector3.Distance(lp.transform.position, _source.transform.position);
            if (d <= VaporMath.DeliveryRadius && d < best) { best = d; receiver = lp; }
        }
        if (receiver != null) EmitTick(receiver);
    }

    /// One condensation tick into a receiver (public: the sim's stand-in for
    /// the delivery-tube run). Consumes the source charge, delivers product.
    public bool EmitTick(LiquidPhysics receiver)
    {
        if (receiver == null || _source == null || _product == null) return false;
        if (!VaporMath.Fires(_source.currentTempC, _requiredC, _source.currentLiquidVolume)) return false;
        if (_source.PourOut(VaporMath.MlPerTick) == null) return false;
        receiver.AddLiquid(_product, VaporMath.MlPerTick);
        return true;
    }
}

/// Completes a WEIGH step ZONE-FREE on the bench balance (Exp 6's "weigh 7 g of
/// each acetate"): the task is done when the vessel has been served its solids
/// AND is resting SETTLED on the balance pan — the balance narrates the grams
/// live the whole time.
public class VesselWeighTask : MonoBehaviour
{
    private ExperimentRunner _runner;
    private string _taskId;
    private LiquidPhysics _lp;
    private LiquidTaskBinding _binding;
    private bool _subscribed;

    public string TaskId => _taskId;

    /// Pure (suite-pinned): served AND settled on the pan.
    public static bool ShouldComplete(bool allReagentsIn, bool settledOnPan)
        => allReagentsIn && settledOnPan;

    public void Bind(ExperimentRunner runner, string taskId, LiquidPhysics lp, LiquidTaskBinding binding)
    {
        if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; }
        _runner = runner; _taskId = taskId; _lp = lp; _binding = binding;
        if (_runner != null) { _runner.ExperimentStarted += OnStarted; _subscribed = true; }
        Register();
    }

    /// Teardown seam (ClearBenchBindings).
    public void Detach()
    { if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; } }

    private void OnDestroy() => Detach();
    private void OnStarted(ExperimentModuleDefinition _) => Register();

    private void Register()
    {
        if (_runner == null || _runner.Graph == null || string.IsNullOrEmpty(_taskId)) return;
        _runner.Graph.RegisterCondition(_taskId, () =>
        {
            var station = FindAnyObjectByType<WeighStation>();
            return _lp != null && station != null
                && ShouldComplete(_binding == null || _binding.ReadyFor(_taskId),
                                  station.SettledWith(_lp));
        });
    }
}
