using UnityEngine;
using System.Collections;

public class TestTaskCompletion : MonoBehaviour
{
    private IEnumerator Start()
    {
        Debug.Log("[TestTaskCompletion] Auto test started.");

        yield return new WaitForSeconds(2f);

        if (ExperimentFlowManager.Instance == null)
        {
            Debug.LogError("[TestTaskCompletion] ExperimentFlowManager.Instance is NULL.");
            yield break;
        }

        CompleteTestTask("fermentation-mixture");
        yield return new WaitForSeconds(2f);

        CompleteTestTask("add-yeast");
        yield return new WaitForSeconds(2f);

        CompleteTestTask("anaerobic-setup");
        yield return new WaitForSeconds(2f);

        CompleteTestTask("fermentation");
        yield return new WaitForSeconds(2f);

        CompleteTestTask("observation-completion");
    }

    private void CompleteTestTask(string taskId)
    {
        Debug.Log("[TestTaskCompletion] Sending task: " + taskId);
        ExperimentFlowManager.Instance.CompleteTask(taskId);
    }
}