using UnityEngine;

public class FireProcedureZone : MonoBehaviour
{
    [SerializeField] private string requiredTagForSafeObject = "fire-safe";
    [SerializeField] private bool triggerOnlyOnce;

    private bool alreadyTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(requiredTagForSafeObject))
        {
            ExperimentFlowManager.Instance?.MarkFireSafetyIncident("Unsafe object near fire zone");
        }

        alreadyTriggered = true;
    }
}
