using UnityEngine;

public class ExperimentTaskTrigger : MonoBehaviour
{
    [SerializeField] private string taskId = "task-id";
    // [SerializeField] private string requiredTag = "Player";
    [SerializeField] private string requiredTag = "";
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
            return;

        if (!string.IsNullOrWhiteSpace(requiredTag) && !other.CompareTag(requiredTag))
            return;

        ExperimentFlowManager.Instance?.CompleteTask(taskId);
        hasTriggered = true;
    }

    public void CompleteTaskFromEvent()
    {
        if (hasTriggered && triggerOnce)
            return;

        ExperimentFlowManager.Instance?.CompleteTask(taskId);
        hasTriggered = true;
    }
}
