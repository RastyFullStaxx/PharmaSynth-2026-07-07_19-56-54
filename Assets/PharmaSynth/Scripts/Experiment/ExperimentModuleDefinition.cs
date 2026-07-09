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

    [Header("Task Checklist (legacy — used by ExperimentFlowManager)")]
    public List<ExperimentTaskDefinition> tasks = new List<ExperimentTaskDefinition>();

    // ---- TaskGraph v2 (plan §4.2) ----------------------------------------

    [Header("TaskGraph (phases A–D, prerequisites, weights)")]
    public List<ExperimentTask> graphTasks = new List<ExperimentTask>();

    [Header("Assessment")]
    [Tooltip("Assessment mode (plan §3.4): Pharmee gives no procedural hints and Dr. Jimenez observes. Off by default — the tutorial + early experiments stay guided.")]
    public bool assessmentMode = false;

    [Header("Mastery (Bayesian Knowledge Tracing)")]
    [Range(0f, 1f), Tooltip("Grade+mastery gate to unlock the next experiment (manuscript = 0.90).")]
    public float masteryThreshold = 0.90f;
    public BktParameters bkt = new BktParameters();
    [Tooltip("Skills tracked for this experiment; leave empty to auto-derive from graphTasks.")]
    public List<LabSkill> trackedSkills = new List<LabSkill>();

    [Header("Scoring")]
    public RubricWeights rubricWeights = new RubricWeights();
    [Min(0f), Tooltip("Par time (seconds) for the time-management rubric criterion.")]
    public float parTimeSeconds = 600f;

    /// Build a fresh runtime TaskGraph from this module's authored tasks.
    public TaskGraph BuildTaskGraph() => new TaskGraph(graphTasks);

    /// Build a fresh BKT model. Uses trackedSkills if set, else the distinct
    /// skills referenced by graphTasks.
    public MasteryModel BuildMasteryModel()
    {
        List<LabSkill> skills = trackedSkills;
        if (skills == null || skills.Count == 0)
        {
            skills = new List<LabSkill>();
            foreach (var t in graphTasks)
                if (t != null && !skills.Contains(t.skill)) skills.Add(t.skill);
        }
        return new MasteryModel(bkt, skills);
    }

    public ScoreCalculator BuildScoreCalculator() => new ScoreCalculator(rubricWeights);
}

[Serializable]
public class ExperimentTaskDefinition
{
    public string taskId = "task-id";
    public string taskLabel = "Task";
    public int scoreValue = 10;
    public bool requiredForCompletion = true;
}
