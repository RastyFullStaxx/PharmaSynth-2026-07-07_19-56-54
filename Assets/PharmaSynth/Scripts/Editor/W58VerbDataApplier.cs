#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// One-shot, idempotent data pass for the W5.8 verb overhaul: re-points the
/// layouts whose tasks are now TOOL verbs (weigh/stir/grind) and wires the
/// scene-side pieces (Methane's hand-built mortar, the matches-box striker).
///   • Acetone: `weigh-acetates` zone-touch → Weigh (scoopula on the pan).
///   • Benzamide: `stand` ("Stir & stand") zone-touch → Stir (glass rod).
///   • Scene: Methane's Eq_Motar/Eq_Pestle get a GrindController completing
///     `prepare-mixture` (dual-path with the legacy zone-touch), and the
///     matches dispenser box becomes a MatchStrikerSurface.
/// (The Aspirin + Caffeine passes were dropped 2026-07-16 with their modules.)
public static class W58VerbDataApplier
{
    const string Dir = "Assets/PharmaSynth/ScriptableObjects/Layouts/";

    [MenuItem("Tools/PharmaSynth/Apply W5.8 Verb Data")]
    public static void Apply()
    {
        if (Application.isPlaying) { Debug.LogWarning("[W58VerbData] exit Play mode first."); return; }
        int changes = 0;

        // ---- Acetone: weigh-acetates zone-touch → Weigh -----------------------
        var acetone = AssetDatabase.LoadAssetAtPath<ExperimentLayout>(Dir + "Layout_Acetone.asset");
        if (acetone != null)
        {
            foreach (var s in acetone.stations)
                if (s.taskId == "weigh-acetates" && s.sim != StationSim.Weigh)
                { s.sim = StationSim.Weigh; changes++; }
            EditorUtility.SetDirty(acetone);
        }

        // ---- Benzamide: stand ("Stir & stand") → Stir -------------------------
        var benzamide = AssetDatabase.LoadAssetAtPath<ExperimentLayout>(Dir + "Layout_Benzamide.asset");
        if (benzamide != null)
        {
            foreach (var s in benzamide.stations)
                if (s.taskId == "stand" && s.sim != StationSim.Stir)
                { s.sim = StationSim.Stir; changes++; }
            EditorUtility.SetDirty(benzamide);
        }

        AssetDatabase.SaveAssets();

        // ---- Scene wiring: Methane mortar grind + matches striker -------------
        int sceneChanges = 0;
        var runner = Object.FindAnyObjectByType<ExperimentRunner>();
        GameObject mortarGo = null, pestleGo = null, matchesBox = null;
        foreach (var li in Object.FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (li.itemId == "Eq_Motar" || li.name.Contains("Motar")) mortarGo = li.gameObject;
            if (li.itemId == "Eq_Pestle" || li.name.Contains("Pestle")) pestleGo = li.gameObject;
        }
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Raw_Matchsticks")) matchesBox = t.gameObject;

        if (mortarGo != null && pestleGo != null)
        {
            var grind = mortarGo.GetComponent<GrindController>();
            if (grind == null)
            {
                grind = mortarGo.AddComponent<GrindController>();
                sceneChanges++;
            }
            grind.Bind(runner, "prepare-mixture", pestleGo.transform);   // dual-path with the zone-touch
        }
        else Debug.LogWarning("[W58VerbData] scene mortar/pestle not found — Methane grind not wired.");

        if (matchesBox != null && matchesBox.GetComponent<MatchStrikerSurface>() == null)
        {
            matchesBox.AddComponent<MatchStrikerSurface>();
            sceneChanges++;
        }

        if (sceneChanges > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }
        Debug.Log($"[W58VerbData] {changes} layout changes, {sceneChanges} scene changes applied (idempotent).");
    }
}
#endif
