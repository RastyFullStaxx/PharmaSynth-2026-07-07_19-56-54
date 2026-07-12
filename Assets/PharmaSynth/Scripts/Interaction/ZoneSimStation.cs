using UnityEngine;

/// Turns a layout zone station into a REAL sustained verb (generalising
/// MethaneApparatusRig to every experiment): while the required prop occupies the
/// zone the bound chemistry sim advances, and the station's task auto-completes via
/// a TaskGraph condition once the sim reaches its target — the player must actually
/// perform and hold the action, not just drop the prop in.
///
/// Attached + configured by ExperimentSceneBuilder for stations whose layout sets a
/// StationSim other than None. Deterministically testable (ForceOccupied + Tick).
public class ZoneSimStation : MonoBehaviour
{
    private ExperimentRunner _runner;
    private string _taskId;
    private StationSim _kind;
    private ZoneItemSensor _sensor;

    private TemperatureSim _temp;
    private CrystallizationController _cryst;
    private FiltrationController _filt;
    private GasCollection _gas;

    [Header("Tuning")]
    public float heatSourceC = 240f;
    public float heatTargetC = 80f;
    public float filtrateMlPerSec = 30f;
    public float gasMlPerSec = 25f;
    [Range(0.1f, 1f)] public float doneFraction = 0.95f;

    private bool _subscribed;
    private SimLoopAudio _loop;
    private StationVfx _vfx;

    /// Optional looping apparatus audio, driven by occupancy (set by the builder).
    public void SetLoopAudio(SimLoopAudio loop) => _loop = loop;

    /// Optional particle effect (steam/frost/drip/bubbles), driven like the audio loop.
    public void SetVfx(StationVfx vfx) => _vfx = vfx;

    /// Optional ignition gate (W5.8): a Heat station whose required prop is a
    /// burner only heats while the burner is LIT (light it with a match). Null
    /// gate = legacy occupancy-only behaviour.
    private System.Func<bool> _ignitionGate;
    public void SetIgnitionGate(System.Func<bool> gate) => _ignitionGate = gate;

    /// Configure at build time. Registers the auto-check condition immediately (if a
    /// graph exists) and re-registers on every ExperimentStarted (Retry-safe).
    public void Bind(ExperimentRunner runner, string taskId, StationSim kind, ZoneItemSensor sensor,
                     TemperatureSim temp, CrystallizationController cryst, FiltrationController filt, GasCollection gas,
                     float heatTarget)
    {
        _runner = runner; _taskId = taskId; _kind = kind; _sensor = sensor;
        _temp = temp; _cryst = cryst; _filt = filt; _gas = gas;
        heatTargetC = heatTarget;
        Resubscribe();
        Register();
    }

    private void OnEnable() => Resubscribe();
    private void OnDisable() { if (_runner != null) _runner.ExperimentStarted -= OnStarted; _subscribed = false; }

    private void Resubscribe()
    {
        if (_runner == null || _subscribed) return;
        _runner.ExperimentStarted += OnStarted;
        _subscribed = true;
    }

    private void OnStarted(ExperimentModuleDefinition _) => Register();

    /// Register the TaskGraph auto-check predicate for this station's verb.
    public void Register()
    {
        if (_runner == null || _runner.Graph == null || string.IsNullOrEmpty(_taskId)) return;
        switch (_kind)
        {
            case StationSim.Heat:
                if (_temp != null) { _temp.ResetSim(); _runner.Graph.RegisterCondition(_taskId, () => _temp != null && _temp.AtLeast(heatTargetC)); }
                break;
            case StationSim.Crystallise:
                if (_cryst != null) { _cryst.ResetProcess(); _runner.Graph.RegisterCondition(_taskId, () => _cryst != null && _cryst.IsDone); }
                break;
            case StationSim.Filter:
                if (_filt != null) { _filt.ResetProcess(); _runner.Graph.RegisterCondition(_taskId, () => _filt != null && _filt.Filtered01(doneFraction)); }
                break;
            case StationSim.Collect:
                if (_gas != null) { _gas.ResetCollection(); _runner.Graph.RegisterCondition(_taskId, () => _gas != null && _gas.Collected(doneFraction)); }
                break;
        }
    }

    private void Update()
    {
        if (_runner == null || !_runner.IsRunning)
        {
            if (_loop != null) _loop.SetRunning(false);
            if (_vfx != null) _vfx.SetRunning(false);
            return;
        }
        Drive(Time.deltaTime, _sensor != null && _sensor.IsOccupied);
    }

    /// Advance the bound sim for one step given zone occupancy (public for tests).
    public void Drive(float dt, bool occupied)
    {
        if (_loop != null) _loop.SetRunning(occupied);
        if (_vfx != null) _vfx.SetRunning(occupied);
        switch (_kind)
        {
            case StationSim.Heat:
                if (_temp != null) _temp.SetHeating(occupied && (_ignitionGate == null || _ignitionGate()), heatSourceC);
                break;
            case StationSim.Crystallise:
                if (occupied && _cryst != null) _cryst.BeginCrystallization();  // ice bath: place & let it set
                break;
            case StationSim.Filter:
                if (occupied && _filt != null) _filt.AddFiltrate(filtrateMlPerSec * Mathf.Max(0f, dt));
                break;
            case StationSim.Collect:
                if (occupied && _gas != null) _gas.AddGas(gasMlPerSec * Mathf.Max(0f, dt));
                break;
        }
    }
}
