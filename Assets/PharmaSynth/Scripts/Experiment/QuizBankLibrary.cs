using System.Collections.Generic;
using UnityEngine;

/// Runtime-safe moduleId → QuizBank lookup (serialized direct references so it
/// works in a build). One asset holds every experiment's post-lab quiz; the
/// PostLabController asks it for the bank matching the running module.
[CreateAssetMenu(fileName = "QuizBankLibrary", menuName = "PharmaSynth/Quiz Bank Library")]
public class QuizBankLibrary : ScriptableObject
{
    public List<QuizBank> banks = new List<QuizBank>();

    public QuizBank GetBank(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return null;
        for (int i = 0; i < banks.Count; i++)
            if (banks[i] != null && banks[i].moduleId == moduleId) return banks[i];
        return null;
    }
}
