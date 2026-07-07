using UnityEngine;

public class EthylProcedureActionUI : MonoBehaviour
{
    public void CompleteFermentationMixture()
{
    if (ExperimentFlowManager.Instance != null)
        ExperimentFlowManager.Instance.CompleteTask("fermentation-mixture");
}

public void CompleteAddYeast()
{
    if (ExperimentFlowManager.Instance != null)
        ExperimentFlowManager.Instance.CompleteTask("add-yeast");
}

public void CompleteAnaerobicSetup()
{
    if (ExperimentFlowManager.Instance != null)
        ExperimentFlowManager.Instance.CompleteTask("anaerobic-setup");
}

public void CompleteFermentation()
{
    if (ExperimentFlowManager.Instance != null)
        ExperimentFlowManager.Instance.CompleteTask("fermentation");
}

public void CompleteObservation()
{
    if (ExperimentFlowManager.Instance != null)
        ExperimentFlowManager.Instance.CompleteTask("observation-completion");
}

}