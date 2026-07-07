using UnityEngine;

/// <summary>
/// Temporary debug tester — attach to any GameObject in the scene.
/// On-screen buttons appear in Play mode to complete each procedure step.
/// Remove this script before final build.
/// </summary>
public class EthylSynthesisDebugTester : MonoBehaviour
{
    private void Start()
    {
        ExperimentFlowManager.Instance?.BeginModule();
        Debug.Log("[DebugTester] Module started. Use the on-screen buttons to complete steps.");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 220));
        GUILayout.Label("=== Ethyl Synthesis Debug ===");

        if (GUILayout.Button("1. Preparation of Fermentation Mixture"))
            Complete("fermentation-mixture", "Preparation of Fermentation Mixture");
        if (GUILayout.Button("2. Addition of Yeast"))
            Complete("add-yeast", "Addition of Yeast");
        if (GUILayout.Button("3. Anaerobic Setup"))
            Complete("anaerobic-setup", "Anaerobic Setup");
        if (GUILayout.Button("4. Fermentation"))
            Complete("fermentation", "Fermentation");
        if (GUILayout.Button("5. Observation of Completion"))
            Complete("observation-completion", "Observation of Completion");

        if (GUILayout.Button("Log Score"))
        {
            var mgr = ExperimentFlowManager.Instance;
            if (mgr != null)
                Debug.Log($"[DebugTester] Score: {mgr.CurrentScore} / {mgr.MaxScore}");
        }

        GUILayout.EndArea();
    }

    private void Complete(string taskId, string label)
    {
        Debug.Log($"[DebugTester] Completing task: {label} ({taskId})");
        ExperimentFlowManager.Instance?.CompleteTask(taskId);
    }
}
