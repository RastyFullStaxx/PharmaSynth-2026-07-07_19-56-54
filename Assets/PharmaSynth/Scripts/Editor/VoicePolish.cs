#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Pre-recording copy pass over the CUTSCENE beats (2026-07-27). Every beat is a
/// SUBTITLE **and** a text-to-speech script, so a line can be perfectly good on
/// screen and wrong in the ear — or, worse, describe apparatus that no longer
/// exists. Run this before spending voice credits: changing a line changes its
/// VoiceLineId, so the manifest must be re-exported afterwards anyway.
///
/// Edits the assets through SerializedObject rather than the YAML, because these
/// subtitles contain ": " (e.g. "Welcome back! Today: Acetone.") which a
/// hand-written scalar silently truncates.
public static class VoicePolish
{
    /// (find, replace, why). Ordered; applied to every CutsceneData beat subtitle.
    static readonly (string find, string replace, string why)[] Rules =
    {
        // ACCURACY — the thermometer was deleted from the scene as VR-inappropriate
        // scaffolding (2026-07-17). Telling the player to watch one is a lie, and
        // Exp 3's cut is driven by the distillation sim, not a readable instrument.
        ("Watch the thermometer through the 70-80 degree window.",
         "Hold the cut steady between 70 and 80 degrees.",
         "thermometer was removed from the lab"),

        // CHEMISTRY — the reagent is Tollens' (Bernhard Tollens); "Tollen's" is a
        // misspelling, and the possessive apostrophe also trips the TTS normaliser.
        ("negative Tollen's", "a negative Tollens test", "Tollens is not a possessive"),

        // TTS — a formula subscript is read letter-by-letter ("em-en-oh-two").
        // Pharmee is speaking aloud; he should say what a demonstrator would say.
        ("filter the MnO2", "filter off the manganese dioxide", "formula read as letters"),
        ("confirm CO2", "confirm carbon dioxide", "formula read as letters"),

        // TTS — a hyphenated range is read as a dash or a subtraction.
        ("the 70-80 degree fraction", "the fraction that comes over between 70 and 80 degrees",
         "hyphen range read as minus"),
    };

    [MenuItem("Tools/PharmaSynth/Voice/Polish Cutscene Copy for Recording")]
    public static void Polish()
    {
        if (Application.isPlaying) { Debug.LogWarning("[VoicePolish] exit Play mode first."); return; }

        var changes = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:CutsceneData",
                     new[] { "Assets/PharmaSynth/ScriptableObjects/Cutscenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cs = AssetDatabase.LoadAssetAtPath<CutsceneData>(path);
            if (cs == null) continue;

            var so = new SerializedObject(cs);
            var beats = so.FindProperty("beats");
            if (beats == null || !beats.isArray) continue;
            bool dirty = false;

            for (int i = 0; i < beats.arraySize; i++)
            {
                var sub = beats.GetArrayElementAtIndex(i).FindPropertyRelative("subtitle");
                if (sub == null || string.IsNullOrEmpty(sub.stringValue)) continue;
                string before = sub.stringValue, after = before;
                foreach (var r in Rules)
                    if (after.Contains(r.find)) after = after.Replace(r.find, r.replace);
                if (after == before) continue;
                sub.stringValue = after;
                dirty = true;
                changes.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " beat " + i + ":\n    - " + before + "\n    + " + after);
            }

            if (dirty) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(cs); }
        }

        AssetDatabase.SaveAssets();
        if (changes.Count == 0)
            Debug.Log("<color=#4CD07D>[VoicePolish] cutscene copy already recording-ready — no changes.</color>");
        else
            Debug.Log($"<color=#4CD07D>[VoicePolish] {changes.Count} beat(s) rewritten for recording:</color>\n"
                      + string.Join("\n", changes)
                      + "\n\nRE-EXPORT the voice manifest now — these lines have new ids.");
    }

    /// Pure (suite): every rule must actually change the text it targets, and the
    /// result must be free of the artefacts we are removing. Guards against a rule
    /// silently going stale when someone rewrites a beat by hand.
    public static string ApplyRules(string subtitle)
    {
        if (string.IsNullOrEmpty(subtitle)) return subtitle;
        string s = subtitle;
        foreach (var r in Rules) if (s.Contains(r.find)) s = s.Replace(r.find, r.replace);
        return s;
    }

    /// Ordinary words that only ever appear capitalised for EMPHASIS. A blanket
    /// all-caps rule is wrong: "PPE" is a genuine initialism and is meant to be
    /// spelled out by the engine, so flagging every caps run would condemn correct
    /// copy. Only real words shouted for stress are the artefact.
    static readonly string[] ShoutedWords =
    { "AND", "OR", "NOT", "ALL", "NEVER", "ALWAYS", "MUST", "ONLY", "BEFORE", "AFTER", "NOW", "DO" };

    /// Pure (suite): copy that is ready to hand to a TTS engine — no formula
    /// subscripts read out as letters, no hyphenated numeric ranges (read as a
    /// dash or a subtraction), no ordinary word shouted in capitals.
    public static bool IsRecordingSafe(string line)
    {
        if (string.IsNullOrEmpty(line)) return true;
        if (line.Contains("MnO2") || line.Contains("CO2") || line.Contains("H2O")
            || line.Contains("H2SO4") || line.Contains("NaOH")) return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(line, @"\d+-\d+")) return false;
        foreach (var w in ShoutedWords)
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"(?<![A-Za-z])" + w + @"(?![A-Za-z])"))
                return false;
        return true;
    }
}
#endif
