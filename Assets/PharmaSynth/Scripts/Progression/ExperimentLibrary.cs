using System.Collections.Generic;
using UnityEngine;

/// A runtime-safe registry of every ExperimentModuleDefinition, referenced directly
/// (serialized asset refs — no Resources/AssetDatabase needed in a build). One asset
/// instance is referenced by the ExperimentLauncher so any of the 11 experiments can
/// be loaded by moduleId from the menu / period hub / experiment-select.
[CreateAssetMenu(fileName = "ExperimentLibrary", menuName = "PharmaSynth/Experiment Library")]
public class ExperimentLibrary : ScriptableObject
{
    public List<ExperimentModuleDefinition> modules = new List<ExperimentModuleDefinition>();

    public ExperimentModuleDefinition Get(string moduleId)
    {
        for (int i = 0; i < modules.Count; i++)
            if (modules[i] != null && modules[i].moduleId == moduleId) return modules[i];
        return null;
    }

    public bool Has(string moduleId) => Get(moduleId) != null;
    public int Count => modules.Count;
}
