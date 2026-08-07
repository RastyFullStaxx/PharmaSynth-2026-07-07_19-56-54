using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// A world location where a procedure step is performed. Completing the station
/// (via interaction, trigger, or Activate()) advances the bound task in the runner.
/// Prerequisite order is enforced by the runner (out-of-order → WrongStep mistake).
public class ExperimentTaskStation : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private string taskId;
    [Header("Auto-activation (optional)")]
    [SerializeField] private bool activateOnSelect = true;     // poke/grab the station to complete it
    [SerializeField] private bool activateOnTriggerEnter = false;
    [SerializeField] private string requiredTag = "";   // e.g. only the player's hand collider
    [Tooltip("If set, only a grabbable LabItem with this exact itemId completes the station (hands-on 'bring the right prop here').")]
    [SerializeField] private string requiredItemId = "";

    private XRBaseInteractable _hookedInteractable;

    public string TaskId => taskId;
    /// Read by the simulated-run harness to verify the required prop exists on the bench.
    public string RequiredItemId => requiredItemId;
    public void SetRunner(ExperimentRunner r) => runner = r;
    public void SetTaskId(string id) => taskId = id;
    public void SetRequiredItemId(string id) => requiredItemId = id;

    /// One-call runtime setup (used by the ExperimentSceneBuilder, which can't use
    /// SerializedObject at play time).
    public void Configure(ExperimentRunner r, string task, string itemId, bool triggerEnter, bool onSelect)
    {
        runner = r; taskId = task; requiredItemId = itemId;
        activateOnTriggerEnter = triggerEnter; activateOnSelect = onSelect;
    }

    private void OnEnable()
    {
        // No self-registration: TutorialTargets' per-run sweep is the single source of
        // truth for taskId → objects. Registering here as well would give the map two
        // lifetimes to keep in sync, and a station disabled mid-run would strand a
        // stale transform the sweep never planted.
        if (activateOnSelect)
        {
            _hookedInteractable = GetComponent<XRBaseInteractable>();
            if (_hookedInteractable != null)
                _hookedInteractable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (_hookedInteractable != null)
            _hookedInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs _) => Activate();

    /// Complete this station's task. Hook to an XR interactable's select event,
    /// a UI button, or call directly.
    public TaskCompletionResult Activate()
    {
        if (runner == null) return TaskCompletionResult.UnknownTask;
        return runner.CompleteTask(taskId);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnTriggerEnter) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        if (!string.IsNullOrEmpty(requiredItemId))
        {
            var item = LabItem.Resolve(other);
            if (item == null || item.itemId != requiredItemId) return;   // wrong prop → ignore
        }
        Activate();
    }

    /// Would the given item complete this station? Pure predicate for edit-mode tests.
    public bool AcceptsItem(LabItem item)
        => string.IsNullOrEmpty(requiredItemId) || (item != null && item.itemId == requiredItemId);
}
