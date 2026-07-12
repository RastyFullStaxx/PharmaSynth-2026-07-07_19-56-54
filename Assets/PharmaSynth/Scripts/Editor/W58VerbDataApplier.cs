#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// One-shot, idempotent data pass for the W5.8 verb overhaul: re-points the
/// layouts whose tasks are now TOOL verbs (weigh/stir/grind) and wires the
/// scene-side pieces (Methane's hand-built mortar, the matches-box striker).
///   • Aspirin: NEW `weigh-salicylic` Weigh station; the flask's pour binding
///     stays expected but stops completing (the balance completes it).
///   • Acetone: `weigh-acetates` zone-touch → Weigh (scoopula on the pan).
///   • Benzamide: `stand` ("Stir & stand") zone-touch → Stir (glass rod).
///   • Caffeine: stages a Mortar + Pestle (educational grind — the module has
///     no grind task; the mechanic still works and teaches).
///   • Scene: Methane's Eq_Motar/Eq_Pestle get a GrindController completing
///     `prepare-mixture` (dual-path with the legacy zone-touch), and the
///     matches dispenser box becomes a MatchStrikerSurface.
public static class W58VerbDataApplier
{
    const string Dir = "Assets/PharmaSynth/ScriptableObjects/Layouts/";

    [MenuItem("Tools/PharmaSynth/Apply W5.8 Verb Data")]
    public static void Apply()
    {
        if (Application.isPlaying) { Debug.LogWarning("[W58VerbData] exit Play mode first."); return; }
        int changes = 0;

        // ---- Aspirin: weigh-salicylic becomes a real weigh --------------------
        var aspirin = AssetDatabase.LoadAssetAtPath<ExperimentLayout>(Dir + "Layout_Aspirin.asset");
        if (aspirin != null)
        {
            bool hasStation = false;
            foreach (var s in aspirin.stations) if (s.taskId == "weigh-salicylic") hasStation = true;
            if (!hasStation)
            {
                aspirin.stations.Insert(0, new ExperimentLayout.Station
                {
                    taskId = "weigh-salicylic",
                    label = "1. Weigh salicylic acid",
                    requiredItemId = "",
                    pos = new Vector3(-0.35f, 0.91f, -2.99f),
                    sim = StationSim.Weigh,
                    simTargetC = 0f,
                });
                changes++;
            }
            foreach (var v in aspirin.vessels)
                foreach (var b in v.bindings)
                    if (b.taskId == "weigh-salicylic" && b.completesTask)
                    { b.completesTask = false; changes++; }
            EditorUtility.SetDirty(aspirin);
        }
        else Debug.LogWarning("[W58VerbData] Layout_Aspirin not found.");

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

        // ---- Caffeine: stage a mortar + pestle (educational grind) ------------
        var caffeine = AssetDatabase.LoadAssetAtPath<ExperimentLayout>(Dir + "Layout_Caffeine.asset");
        if (caffeine != null)
        {
            bool hasMortar = false, hasPestle = false;
            foreach (var p in caffeine.props)
            {
                if (p.prefabName == "Motar") hasMortar = true;
                if (p.prefabName == "Pestle") hasPestle = true;
            }
            if (!hasMortar)
            {
                caffeine.props.Add(new ExperimentLayout.Prop
                {
                    prefabName = "Motar", itemId = "mortar", displayName = "Mortar",
                    pos = new Vector3(-1.7f, 0.91f, -3.55f), targetHeight = 0.12f,
                });
                changes++;
            }
            if (!hasPestle)
            {
                caffeine.props.Add(new ExperimentLayout.Prop
                {
                    prefabName = "Pestle", itemId = "pestle", displayName = "Pestle",
                    pos = new Vector3(-1.55f, 0.91f, -3.55f), targetHeight = 0.12f,
                });
                changes++;
            }
            EditorUtility.SetDirty(caffeine);
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
