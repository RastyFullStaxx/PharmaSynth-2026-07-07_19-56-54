#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Wires Pharmee's robot voice colouring onto HIS narration channel only
/// (2026-07-27). Dr. Jimenez is a human examiner and must stay untouched — the
/// whole point of the two-NPC contrast is that one is a machine and one is not.
///
/// Ring modulation does the robot work (RobotVoiceFx); Unity's stock filters do
/// the colouring — a band limit so he sounds like he is coming through a speaker
/// grille, and a short chorus for the metallic doubling. All of it is live-tunable
/// in the Inspector during Play, so the character can be dialled in by ear without
/// regenerating a single clip.
public static class RobotVoiceBuilder
{
    const string Menu = "Tools/PharmaSynth/Voice/";
    const string ProfilePath = "Assets/PharmaSynth/ScriptableObjects/RobotVoiceProfile.asset";

    /// Load-or-create the ONE shared tuning asset every Pharmee channel reads.
    static RobotVoiceProfile EnsureProfile()
    {
        var p = AssetDatabase.LoadAssetAtPath<RobotVoiceProfile>(ProfilePath);
        if (p != null) return p;
        p = ScriptableObject.CreateInstance<RobotVoiceProfile>();
        AssetDatabase.CreateAsset(p, ProfilePath);
        AssetDatabase.SaveAssets();
        return p;
    }

    [MenuItem(Menu + "Robotise Pharmee's Voice")]
    public static void Apply()
    {
        if (Application.isPlaying) { Debug.LogWarning("[RobotVoice] exit Play mode first."); return; }

        var profile = EnsureProfile();
        int done = 0;
        foreach (var src in PharmeeVoiceSources())
        {
            Undo.RegisterFullObjectHierarchyUndo(src.gameObject, "Robotise Pharmee");

            var fx = src.GetComponent<RobotVoiceFx>() ?? src.gameObject.AddComponent<RobotVoiceFx>();
            fx.profile = profile;   // every channel reads the SAME asset

            // Speaker-grille band. Keeping the top at 5 kHz preserves sibilance,
            // which is what carries consonants — cut lower and he gets muddy.
            var hp = src.GetComponent<AudioHighPassFilter>() ?? src.gameObject.AddComponent<AudioHighPassFilter>();
            hp.cutoffFrequency = 260f;
            var lp = src.GetComponent<AudioLowPassFilter>() ?? src.gameObject.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 5000f;

            // Metallic doubling. Short delay + shallow depth: a long/deep chorus
            // sounds like a choir, not a machine.
            var ch = src.GetComponent<AudioChorusFilter>() ?? src.gameObject.AddComponent<AudioChorusFilter>();
            ch.delay = 20f; ch.rate = 0.9f; ch.depth = 0.16f;
            ch.dryMix = 0.62f; ch.wetMix1 = 0.38f; ch.wetMix2 = 0.18f; ch.wetMix3 = 0f;

            // A whisper of grit. Past ~0.2 it turns to fuzz and eats consonants.
            var dist = src.GetComponent<AudioDistortionFilter>() ?? src.gameObject.AddComponent<AudioDistortionFilter>();
            dist.distortionLevel = 0.12f;

            // NEVER pitch-shift speech on an AudioSource: Unity's pitch also changes
            // SPEED, so it would make him gabble rather than sound synthetic.
            src.pitch = 1f;

            EditorUtility.SetDirty(src.gameObject);
            done++;
        }

        if (done == 0)
        {
            Debug.LogError("[RobotVoice] no Pharmee narration AudioSource found — is the scene open, "
                         + "and has 'Wire NPC Polish' run?");
            return;
        }
        Save();
        Debug.Log($"<color=#4CD07D>[RobotVoice] robot colouring applied to {done} Pharmee voice source(s), all reading "
                  + $"the ONE shared profile at {ProfilePath}. Select that asset and tune ringHz / ringMix — during Play "
                  + "if you like, the values stick. It reaches every Pharmee line, generated or not. "
                  + "Dr. Jimenez untouched.</color>");
    }

    [MenuItem(Menu + "Remove Pharmee Robot Voice (A/B)")]
    public static void Remove()
    {
        if (Application.isPlaying) { Debug.LogWarning("[RobotVoice] exit Play mode first."); return; }
        int done = 0;
        foreach (var src in PharmeeVoiceSources())
        {
            foreach (var c in new Component[]
            {
                src.GetComponent<RobotVoiceFx>(), src.GetComponent<AudioHighPassFilter>(),
                src.GetComponent<AudioLowPassFilter>(), src.GetComponent<AudioChorusFilter>(),
                src.GetComponent<AudioDistortionFilter>(),
            })
                if (c != null) { Object.DestroyImmediate(c); done++; }
        }
        Save();
        Debug.Log($"[RobotVoice] removed {done} component(s) — Pharmee is back to the raw read.");
    }

    /// Pharmee's narration AudioSources: every NPCNarrationController in the scene
    /// EXCEPT the ones under Dr. Jimenez. Matches VoiceImportTool's own rule, so a
    /// channel can never be robotised and voiced as Jimenez at the same time.
    static System.Collections.Generic.List<AudioSource> PharmeeVoiceSources()
    {
        var list = new System.Collections.Generic.List<AudioSource>();
        foreach (var n in Object.FindObjectsByType<NPCNarrationController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (n == null || IsJimenez(n.transform)) continue;
            var so = new SerializedObject(n);
            var src = so.FindProperty("narratorAudioSource")?.objectReferenceValue as AudioSource;
            if (src == null) src = n.GetComponent<AudioSource>();
            if (src != null && !list.Contains(src)) list.Add(src);
        }
        return list;
    }

    static bool IsJimenez(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.Contains("jimenez") || n.Contains("examiner") || n.Contains("proctor")) return true;
        }
        return false;
    }

    static void Save()
    {
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }
}
#endif
