using System.Collections.Generic;
using UnityEngine;

public class ProcedureChecklistUI : MonoBehaviour
{
    [System.Serializable]
    public class ChecklistItem
    {
        public string taskId;
        public GameObject checkmark;
    }

    [Header("Checklist Items")]
    [SerializeField] private List<ChecklistItem> checklistItems = new List<ChecklistItem>();

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    private void Start()
    {
        HideAllCheckmarks();

        if (ExperimentFlowManager.Instance == null)
        {
            Debug.LogError("[ProcedureChecklistUI] ExperimentFlowManager.Instance is NULL.");
            return;
        }

        ExperimentFlowManager.Instance.TaskCompleted += OnTaskCompleted;

        if (enableDebug)
            Debug.Log("[ProcedureChecklistUI] Listening to ExperimentFlowManager TaskCompleted event.");
    }

    private void OnDestroy()
    {
        if (ExperimentFlowManager.Instance != null)
        {
            ExperimentFlowManager.Instance.TaskCompleted -= OnTaskCompleted;

            if (enableDebug)
                Debug.Log("[ProcedureChecklistUI] Stopped listening to TaskCompleted event.");
        }
    }

    private void HideAllCheckmarks()
    {
        foreach (ChecklistItem item in checklistItems)
        {
            if (item.checkmark != null)
            {
                item.checkmark.SetActive(false);
            }
            else if (enableDebug)
            {
                Debug.LogWarning("[ProcedureChecklistUI] Missing checkmark for taskId: " + item.taskId);
            }
        }
    }

    private void OnTaskCompleted(string taskId)
    {
        if (enableDebug)
            Debug.Log("[ProcedureChecklistUI] Checklist received completed task: " + taskId);

        foreach (ChecklistItem item in checklistItems)
        {
            if (item.taskId == taskId)
            {
                if (item.checkmark != null)
                {
                    item.checkmark.SetActive(true);

                    if (enableDebug)
                        Debug.Log("[ProcedureChecklistUI] Checkmark enabled for: " + taskId);
                }
                else
                {
                    Debug.LogWarning("[ProcedureChecklistUI] Checkmark is missing for: " + taskId);
                }

                return;
            }
        }

        Debug.LogWarning("[ProcedureChecklistUI] No checklist item found for taskId: " + taskId);
    }
}