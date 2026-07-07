using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Kicks off (or restarts) an experiment — hook to the intro-cutscene "Start"
/// button or a begin trigger (poke/grab an XR interactable on this object). Also
/// serves the grade screen's Retry.
public class ExperimentStarter : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private bool clearStationRegistryOnBegin = true;
    [SerializeField] private bool beginOnSelect = true;

    private XRBaseInteractable _hooked;

    public void SetRunner(ExperimentRunner r) => runner = r;

    private void OnEnable()
    {
        if (!beginOnSelect) return;
        _hooked = GetComponent<XRBaseInteractable>();
        if (_hooked != null) _hooked.selectEntered.AddListener(OnSelect);
    }

    private void OnDisable()
    {
        if (_hooked != null) _hooked.selectEntered.RemoveListener(OnSelect);
    }

    private void OnSelect(SelectEnterEventArgs _) => Begin();

    public void Begin()
    {
        if (runner == null) return;
        if (clearStationRegistryOnBegin) ExperimentStationRegistry.Clear();
        runner.StartExperiment();
    }

    public void Retry() => Begin();
}
