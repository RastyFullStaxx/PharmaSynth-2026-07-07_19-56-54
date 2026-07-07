using UnityEngine;

public class ExperimentErrorReporter : MonoBehaviour
{
    [SerializeField] private string contextName = "Procedure";

    public void ReportWrongStep()
    {
        ExperimentFlowManager.Instance?.MarkWrongStep(contextName);
    }

    public void ReportWrongStep(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            details = contextName;

        ExperimentFlowManager.Instance?.MarkWrongStep(details);
    }

    public void ReportFireIncident(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            details = contextName;

        ExperimentFlowManager.Instance?.MarkFireSafetyIncident(details);
    }
}
