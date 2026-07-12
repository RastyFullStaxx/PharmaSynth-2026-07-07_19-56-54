using UnityEngine;

/// Turns Methane's heating/collection steps into REAL verbs instead of zone-touches:
/// while the burner prop sits in its zone the apparatus heats (TemperatureSim);
/// once hot, methane bubbles into the collection tube while it is held in its zone
/// (GasCollection). The heat-mixture / collect-gas tasks then complete via TaskGraph
/// auto-check conditions — the player must actually perform and sustain the action.
public class MethaneApparatusRig : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private TemperatureSim temperature;
    [SerializeField] private GasCollection gas;
    [SerializeField] private ZoneItemSensor burnerZone;    // on Station_HeatMixture
    [SerializeField] private ZoneItemSensor collectZone;   // on Station_CollectGas

    [Header("Tuning")]
    [SerializeField] private float burnerSourceC = 220f;
    [SerializeField] private float heatDoneC = 120f;
    [SerializeField] private float gasMlPerSecond = 22f;      // ~5 s to fill once hot
    [SerializeField, Range(0.1f, 1f)] private float collectedFraction = 0.9f;

    private const string ModuleId = "tutorial-methane";
    private bool _active;
    private bool _prevHeating;      // burner-ignite flame edge
    private bool _splintFired;      // splint flame-test pops once per collection
    private float _collectedAt = -1f;   // when the tube filled (W5.8 splint timing)
    public const float SplintMatchDistance = 0.25f;
    public const float SplintAutoSeconds = 20f;

    /// Pure (W5.8): the splint test fires when a LIT match reaches the filled
    /// tube — or automatically after a grace period so nothing ever stalls.
    public static bool SplintShouldFire(bool collected, bool alreadyFired, float matchDistance, bool matchLit, float sinceCollected)
        => collected && !alreadyFired
           && ((matchLit && matchDistance <= SplintMatchDistance) || sinceCollected >= SplintAutoSeconds);

    /// Edit-mode/test binding (OnEnable doesn't fire on AddComponent in edit mode).
    public void Bind(ExperimentRunner r, TemperatureSim t, GasCollection g,
                     ZoneItemSensor burner, ZoneItemSensor collect)
    {
        runner = r; temperature = t; gas = g; burnerZone = burner; collectZone = collect;
        Resubscribe();
    }

    private void OnEnable() => Resubscribe();

    private void OnDisable()
    {
        if (runner != null) runner.ExperimentStarted -= HandleExperimentStarted;
    }

    private void Resubscribe()
    {
        if (runner == null) return;
        runner.ExperimentStarted -= HandleExperimentStarted;
        runner.ExperimentStarted += HandleExperimentStarted;
    }

    /// Registers the world-state completion conditions for a fresh Methane attempt.
    /// Public so edit-mode tests can invoke it directly.
    public void HandleExperimentStarted(ExperimentModuleDefinition module)
    {
        _active = module != null && module.moduleId == ModuleId
                  && runner != null && runner.Graph != null;
        _prevHeating = false; _splintFired = false;
        if (!_active) return;

        if (temperature != null)
        {
            temperature.ResetSim();
            runner.Graph.RegisterCondition("heat-mixture", () => temperature != null && temperature.AtLeast(heatDoneC));
        }
        if (gas != null)
        {
            gas.ResetCollection();
            runner.Graph.RegisterCondition("collect-gas", () => gas != null && gas.Collected(collectedFraction));
        }
    }

    private void Update()
    {
        if (!_active || runner == null || !runner.IsRunning) return;

        // Burner in the heating zone → flame on; removed → cools back down.
        bool heating = burnerZone != null && burnerZone.IsOccupied;
        if (temperature != null) temperature.SetHeating(heating, burnerSourceC);
        if (heating && !_prevHeating && burnerZone != null)       // ignite puff
            EffectVfx.FlamePop(burnerZone.transform.position + Vector3.up * 0.08f);
        _prevHeating = heating;

        // Hot apparatus + collection tube held in place → gas accumulates.
        bool hot = temperature != null && temperature.AtLeast(heatDoneC);
        if (hot && gas != null && collectZone != null && collectZone.IsOccupied)
            gas.AddGas(gasMlPerSecond * Time.deltaTime);

        // Splint flame test (W5.8: interactive): once the tube fills, bring a LIT
        // match to it and the methane pops — the real manuscript verb. An auto
        // fallback still fires after a grace period so the tutorial never stalls.
        bool collected = gas != null && gas.Collected(collectedFraction);
        if (collected && _collectedAt < 0f) _collectedAt = Time.time;
        if (!collected) _collectedAt = -1f;
        if (!_splintFired && collected && collectZone != null)
        {
            float best = float.MaxValue; bool anyLit = false;
            foreach (var m in FindObjectsByType<Matchstick>(FindObjectsSortMode.None))
            {
                if (!m.IsLit) continue;
                anyLit = true;
                best = Mathf.Min(best, Vector3.Distance(m.transform.position, collectZone.transform.position));
            }
            if (SplintShouldFire(true, _splintFired, anyLit ? best : float.MaxValue, anyLit, Time.time - _collectedAt))
            {
                EffectVfx.FlamePop(collectZone.transform.position + Vector3.up * 0.1f);
                FloatingText.Show("Pop! Methane confirmed", collectZone.transform.position + Vector3.up * 0.2f, new Color(0.7f, 1f, 0.7f));
                _splintFired = true;
            }
        }
    }
}
