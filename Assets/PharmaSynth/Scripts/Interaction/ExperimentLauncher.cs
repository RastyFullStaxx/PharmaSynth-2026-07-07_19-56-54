using UnityEngine;
using UnityEngine.Events;

/// Loads any of the 11 experiments into the lab scene by moduleId: it swaps the
/// ExperimentRunner's active module (from the ExperimentLibrary) and starts a fresh
/// attempt. The menu / period hub / experiment-select call Launch(); on lab-scene
/// entry it can auto-launch whatever GameFlow.SelectedModuleId holds.
public class ExperimentLauncher : MonoBehaviour
{
    [SerializeField] private ExperimentLibrary library;
    [SerializeField] private ExperimentRunner runner;
    [Tooltip("Launch GameFlow.SelectedModuleId automatically when this scene loads.")]
    [SerializeField] private bool launchSelectedOnStart = false;

    [Tooltip("Raised after a module is loaded, so scene wiring (stations/props/cutscenes) can rebuild for it.")]
    public UnityEvent<ExperimentModuleDefinition> onModuleLoaded;

    public ExperimentLibrary Library => library;
    public void SetLibrary(ExperimentLibrary l) => library = l;
    public void SetRunner(ExperimentRunner r) => runner = r;

    private void Start()
    {
        if (launchSelectedOnStart) LaunchSelected();
    }

    public ExperimentModuleDefinition LaunchSelected() => Launch(GameFlow.SelectedModuleId);

    /// Swap the runner to the requested module and begin a fresh attempt.
    /// Returns the loaded module, or null if unknown / unwired.
    public ExperimentModuleDefinition Launch(string moduleId)
    {
        if (library == null || runner == null)
        {
            Debug.LogWarning("[ExperimentLauncher] Library or runner not assigned.");
            return null;
        }
        var mod = library.Get(moduleId);
        if (mod == null)
        {
            Debug.LogWarning("[ExperimentLauncher] Unknown moduleId: " + moduleId);
            return null;
        }
        runner.SetModule(mod);
        onModuleLoaded?.Invoke(mod);
        runner.StartExperiment();
        return mod;
    }
}
