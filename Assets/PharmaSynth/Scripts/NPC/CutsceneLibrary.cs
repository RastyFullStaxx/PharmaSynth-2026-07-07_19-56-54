using System;
using System.Collections.Generic;
using UnityEngine;

/// moduleId → the four cutscenes for that experiment (Intro, ReagentPrep, Success,
/// Failure). Lets the single scene CutsceneDirector serve all 11 experiments: on
/// ExperimentStarted it swaps its set from here instead of holding one hand-wired set.
[CreateAssetMenu(fileName = "CutsceneLibrary", menuName = "PharmaSynth/Cutscene Library")]
public class CutsceneLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string moduleId;
        public CutsceneData intro;
        public CutsceneData reagentPrep;
        public CutsceneData success;
        public CutsceneData failure;

        public bool IsComplete => intro != null && reagentPrep != null && success != null && failure != null;
    }

    public List<Entry> entries = new List<Entry>();

    public Entry GetSet(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].moduleId == moduleId) return entries[i];
        return null;
    }
}
