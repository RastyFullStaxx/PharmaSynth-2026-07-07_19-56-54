using UnityEditor;
using UnityEngine;

public static class EthylSynthesisModuleCreator
{
    [MenuItem("VR ChemLab/Create Ethyl Alcohol Synthesis Module")]
    public static void CreateModule()
    {
        ExperimentModuleDefinition module = ScriptableObject.CreateInstance<ExperimentModuleDefinition>();

        module.moduleId = "ethyl-alcohol-synthesis";
        module.moduleTitle = "Ethyl Alcohol Synthesis";

        module.intendedLearningOutcomes = new System.Collections.Generic.List<string>
        {
            "Understand the fermentation process for ethanol production.",
            "Apply safe laboratory handling when working with biological agents.",
            "Interpret CO2 production as evidence of anaerobic fermentation.",
            "Identify completion of fermentation by observing reaction changes."
        };

        module.tasks = new System.Collections.Generic.List<ExperimentTaskDefinition>
        {
            new ExperimentTaskDefinition
            {
                taskId    = "fermentation-mixture",
                taskLabel = "Preparation of Fermentation Mixture",
                scoreValue            = 20,
                requiredForCompletion = true
            },
            new ExperimentTaskDefinition
            {
                taskId    = "add-yeast",
                taskLabel = "Addition of Yeast",
                scoreValue            = 20,
                requiredForCompletion = true
            },
            new ExperimentTaskDefinition
            {
                taskId    = "anaerobic-setup",
                taskLabel = "Anaerobic Setup",
                scoreValue            = 20,
                requiredForCompletion = true
            },
            new ExperimentTaskDefinition
            {
                taskId    = "fermentation",
                taskLabel = "Fermentation",
                scoreValue            = 20,
                requiredForCompletion = true
            },
            new ExperimentTaskDefinition
            {
                taskId    = "observation-completion",
                taskLabel = "Observation of Completion",
                scoreValue            = 20,
                requiredForCompletion = true
            }
        };

        string folder = "Assets/Resources/Experiments";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Experiments");

        string assetPath = folder + "/EthylAlcoholSynthesis.asset";
        AssetDatabase.CreateAsset(module, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = module;

        Debug.Log("[VR ChemLab] EthylAlcoholSynthesis module asset created at: " + assetPath);
    }
}
