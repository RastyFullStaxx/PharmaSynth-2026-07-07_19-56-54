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

        // Dropper verb (2026-07-16): DropperController was authored for Exp 2 but
        // never ATTACHED to anything — the user grabbed Eq_Dropper in the headset and
        // nothing responded, because no component was listening (0 in the scene).
        // Every Eq_Dropper* gets the verb: grip to hold, touch a bottle to draw,
        // TRIGGER to release exactly one counted drop.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("Eq_Dropper")) continue;
            if (t.GetComponent<DropperController>() == null)
            {
                var d = t.gameObject.AddComponent<DropperController>();
                d.Bind(t.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>());
                sceneChanges++;
            }
        }

        // Funnels are liquid PASS-THROUGH (2026-07-17): without the marker the
        // pour ray landed ON the funnel's collider and wasted the stream as a
        // puddle there — a filter pour never reached the beaker underneath.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("Eq_Funnel") && !t.name.StartsWith("Funnel_")) continue;
            if (t.GetComponent<LiquidPassthrough>() == null)
            {
                t.gameObject.AddComponent<LiquidPassthrough>();
                sceneChanges++;
            }
        }

        // The WATER BATH is a zone-free tool (2026-07-17): fill it with distilled
        // water, put a lit burner beside it, and it warms any vessel brought
        // close — anywhere in the lab. Needs a liquid receiver (the water the
        // player pours), a heat model, its live label, and the controller.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "WaterBath") continue;
            var host = t.gameObject;
            // LiquidPhysics requires a Renderer host — use the bath root only if
            // it renders, else the first mesh child (colliders find it via parent).
            if (host.GetComponent<Renderer>() == null)
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                { host = r.gameObject; break; }
            var blp = host.GetComponent<LiquidPhysics>();
            if (blp == null)
            {
                blp = host.AddComponent<LiquidPhysics>();
                blp.maxVolume = 400f;
                blp.SetContents(null, 0f);   // starts EMPTY — the player fills it
                sceneChanges++;
            }
            var bts = t.GetComponent<TemperatureSim>();
            if (bts == null)
            {
                bts = t.gameObject.AddComponent<TemperatureSim>();
                var so = new SerializedObject(bts);
                so.FindProperty("ambientC").floatValue = 25f;
                so.FindProperty("targetC").floatValue = 95f;
                so.FindProperty("overheatC").floatValue = 999f;   // a WATER bath cannot ruin a batch
                so.ApplyModifiedPropertiesWithoutUndo();
                sceneChanges++;
            }
            var blabel = t.GetComponent<ProximityLabel>() ?? t.gameObject.AddComponent<ProximityLabel>();
            var wbc = t.GetComponent<WaterBathController>();
            if (wbc == null) { wbc = t.gameObject.AddComponent<WaterBathController>(); sceneChanges++; }
            wbc.Bind(blp, bts, blabel);
            blabel.SetLabel(WaterBathMath.StatusLine(false, false, 25f), 1.6f);
        }

        // The porcelain spatula is the FINE solids tool: Exp 2 weighs 0.1 g salicylic
        // and 0.5 g aspirin, both smaller than the scoopula's 2 g dip. Its charge is
        // ScoopMath.GramsPerSpatula; the scoopula keeps the coarse 2 g.
        var spatula = GameObject.Find("Eq_PorcelainSpatula");
        var spatScoop = spatula != null ? spatula.GetComponent<ScoopController>() : null;
        if (spatScoop != null)
        {
            spatScoop.SetGramsPerDip(ScoopMath.GramsPerSpatula);
            EditorUtility.SetDirty(spatScoop);
            sceneChanges++;
        }
        else Debug.LogWarning("[W58VerbData] Eq_PorcelainSpatula/ScoopController not found — Exp 2's 0.1 g dips not wired.");

        if (sceneChanges > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }
        Debug.Log($"[W58VerbData] {changes} layout changes, {sceneChanges} scene changes applied (idempotent).");
    }
}
#endif
