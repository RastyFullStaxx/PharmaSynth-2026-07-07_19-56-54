using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExperimentModule", menuName = "VR ChemLab/Experiment Module")]
public class ExperimentModuleDefinition : ScriptableObject
{
    [Header("Module")]
    public string moduleId = "module-1";
    public string moduleTitle = "Chemical Experiment";

    [Header("Intended Learning Outcomes")]
    [TextArea(2, 5)]
    public List<string> intendedLearningOutcomes = new List<string>();

    [Header("Task Checklist")]
    public List<ExperimentTaskDefinition> tasks = new List<ExperimentTaskDefinition>();
}

[Serializable]
public class ExperimentTaskDefinition
{
    public string taskId = "task-id";
    public string taskLabel = "Task";
    public int scoreValue = 10;
    public bool requiredForCompletion = true;
}
