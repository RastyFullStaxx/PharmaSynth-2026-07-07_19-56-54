using UnityEngine;

/// Pure litmus colour response (manuscript: pH checks in Exp 3, 4, 8).
public static class LitmusMath
{
    public static readonly Color AcidRed = new Color(0.85f, 0.2f, 0.18f);
    public static readonly Color NeutralViolet = new Color(0.62f, 0.45f, 0.65f);
    public static readonly Color BaseBlue = new Color(0.2f, 0.35f, 0.85f);

    /// Blue litmus turns red at/below this — also the completion gate for
    /// litmus-confirmation tasks (Exp 4's "blue litmus turns red").
    public const float AcidPH = 4.5f;

    /// Red below ~4.5, blue above ~8.3, graded violet between.
    public static Color ColorForPH(float pH)
    {
        if (pH <= AcidPH) return AcidRed;
        if (pH >= 8.3f) return BaseBlue;
        float t = Mathf.InverseLerp(AcidPH, 8.3f, pH);
        return t < 0.5f
            ? Color.Lerp(AcidRed, NeutralViolet, t * 2f)
            : Color.Lerp(NeutralViolet, BaseBlue, (t - 0.5f) * 2f);
    }

    /// Mixture pH: the component farther from neutral dominates (an acid stays
    /// acidic under any amount of water). Feeds LiquidPhysics.CurrentPH so the
    /// strip reads the MIXTURE, not whichever chemical happened to land first.
    public static float DominantPH(float a, float b)
        => Mathf.Abs(b - 7f) > Mathf.Abs(a - 7f) ? b : a;
}

/// A grabbable litmus strip: touch it to any liquid (trigger or collision with a
/// vessel holding a chemical) and it tints to the mixture's pH — one-shot, like
/// the real thing. Built by the cabinet builder's consumables box.
public class LitmusStrip : MonoBehaviour
{
    [SerializeField] private Renderer strip;
    private bool _used;
    private MaterialPropertyBlock _mpb;

    public bool Used => _used;

    public void Bind(Renderer r) => strip = r;

    private void OnTriggerEnter(Collider other) => TryRead(other);
    private void OnCollisionEnter(Collision c) => TryRead(c.collider);

    private void TryRead(Collider other)
    {
        if (other == null) return;
        TouchVessel(other.GetComponentInParent<LiquidPhysics>());
    }

    /// The touch itself, physics-free (SimulatedRun's stand-in for physically
    /// dipping the strip — mirrors WaterBathController.HeatVessel). Reads the
    /// MIXTURE pH and tells the vessel's litmus task, if it carries one.
    public void TouchVessel(LiquidPhysics lp)
    {
        if (_used) return;
        if (lp == null || lp.currentChemical == null || lp.currentLiquidVolume <= 0.5f) return;
        Apply(lp.CurrentPH);
        var task = lp.GetComponentInParent<VesselLitmusTask>();
        if (task != null) task.NotifyRead(lp.CurrentPH);
    }

    /// Public + pure-drivable for tests and the methane-style rigs.
    public void Apply(float pH)
    {
        _used = true;
        if (strip == null) strip = GetComponentInChildren<Renderer>();
        if (strip == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Color c = LitmusMath.ColorForPH(pH);
        _mpb.SetColor("_BaseColor", c);
        _mpb.SetColor("_Color", c);
        strip.SetPropertyBlock(_mpb);
    }
}

/// Completes a litmus-confirmation step ZONE-FREE (Exp 4's "blue litmus turns
/// red"): the task is done when this vessel has been served its reagents AND a
/// litmus strip actually touched it while the mixture read ACID — the strip
/// turning red IS the completion, wherever the player does it. A neutral read
/// (water first, nothing dissolved yet) marks nothing; touch again with a fresh
/// strip once the product is in.
public class VesselLitmusTask : MonoBehaviour
{
    private ExperimentRunner _runner;
    private string _taskId;
    private LiquidTaskBinding _binding;
    private LiquidPhysics _lp;
    private bool _readAcidic;
    private bool _subscribed;

    public string TaskId => _taskId;

    /// Pure (suite-pinned): served AND the strip really turned red.
    public static bool ShouldComplete(bool allReagentsIn, bool stripReadAcid)
        => allReagentsIn && stripReadAcid;

    /// Called by the strip that touched this vessel.
    public void NotifyRead(float pH)
    {
        if (pH <= LitmusMath.AcidPH && !_readAcidic)
        {
            _readAcidic = true;
            // Assurance cue: the colour change is small — say what it means.
            if (Application.isPlaying)
                FloatingText.Show("Blue litmus turns RED — acid confirmed",
                                  transform.position + Vector3.up * 0.15f, LitmusMath.AcidRed, 1f);
        }
        if (_runner != null && _runner.Graph != null) _runner.Graph.Tick();
    }

    public void Bind(ExperimentRunner runner, string taskId, LiquidTaskBinding binding, LiquidPhysics lp)
    {
        if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; }
        _runner = runner; _taskId = taskId; _binding = binding; _lp = lp;
        _readAcidic = false;
        if (_runner != null) { _runner.ExperimentStarted += OnStarted; _subscribed = true; }
        Register();
    }

    /// Teardown seam (ClearBenchBindings) — bench items survive rebuilds.
    public void Detach()
    { if (_runner != null && _subscribed) { _runner.ExperimentStarted -= OnStarted; _subscribed = false; } }

    private void OnDestroy() => Detach();
    private void OnStarted(ExperimentModuleDefinition _) { _readAcidic = false; Register(); }

    private void Register()
    {
        if (_runner == null || _runner.Graph == null || string.IsNullOrEmpty(_taskId)) return;
        _runner.Graph.RegisterCondition(_taskId, () =>
            _binding != null && ShouldComplete(_binding.ReadyFor(_taskId), _readAcidic));
    }
}
