#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Tutorial Mode scene wiring (2026-08-07). Mirrors DemoModeBuilder's shape so the
/// two special modes are built the same way:
///   • Build Tutorial Menu Button — MainMenu scene: clones the Laboratory button
///     into a "Tutorial" button wired to MainMenuController.OnTutorialLaboratory.
///     Unlike the Demo button this one is ALWAYS visible — practice mode is a
///     shipped feature, not a config-gated demo affordance.
/// Idempotent: re-running re-labels and re-wires the existing button in place.
public static class TutorialModeBuilder
{
    // Cyan-green, deliberately NOT the demo amber — a player must never confuse
    // "unlocked practice" with "unlocked demo build".
    static readonly Color TutorialAccent = new Color(0.45f, 0.95f, 0.75f);

    [MenuItem("Tools/PharmaSynth/Build Tutorial Menu Button")]
    public static void BuildMenuButton()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TutorialModeBuilder] exit Play mode first."); return; }
        var lab = FindInScene("LaboratoryButton");
        var controller = Object.FindAnyObjectByType<MainMenuController>();
        if (lab == null || controller == null)
        {
            Debug.LogError("[TutorialModeBuilder] LaboratoryButton/MainMenuController not found — open MainMenu.unity first.");
            return;
        }

        var parent = lab.transform.parent;
        var existing = parent.Find("TutorialModeButton");
        GameObject tut = existing != null ? existing.gameObject : Object.Instantiate(lab, parent);
        tut.name = "TutorialModeButton";

        // Tutorial takes the last SHIPPED slot and pushes the config-gated Demo
        // button below it. DemoModeBuilder places Demo at "lowest + one row", so
        // appending after it would strand Tutorial with a visible gap above it in
        // any build where DemoButtonVisibility keeps Demo hidden — which is every
        // shipped build. Position is recomputed on each run, so the two builders
        // can be re-run in either order without drifting.
        var labRt = lab.GetComponent<RectTransform>();
        float rowH = labRt.sizeDelta.y > 1f ? labRt.sizeDelta.y : 60f;
        float gap = rowH + 14f;

        var demoT = parent.Find("DemoModeButton");
        float minY = labRt.anchoredPosition.y;
        foreach (var b in parent.GetComponentsInChildren<Button>(true))
        {
            var rt = b.GetComponent<RectTransform>();
            if (rt == null || rt.parent != parent) continue;
            if (rt.gameObject == tut) continue;                             // don't chase ourselves
            if (demoT != null && rt.gameObject == demoT.gameObject) continue; // demo sits BELOW us
            if (rt.anchoredPosition.y < minY) minY = rt.anchoredPosition.y;
        }

        var tutRt = tut.GetComponent<RectTransform>();
        tutRt.anchoredPosition = new Vector2(labRt.anchoredPosition.x, minY - gap);
        if (demoT != null)
        {
            var demoRt = demoT.GetComponent<RectTransform>();
            if (demoRt != null)
                demoRt.anchoredPosition = new Vector2(labRt.anchoredPosition.x, tutRt.anchoredPosition.y - gap);
        }

        var label = tut.GetComponentInChildren<TMP_Text>(true);
        if (label != null) { label.text = "Tutorial"; label.color = TutorialAccent; }

        var btn = tut.GetComponent<Button>();
        if (btn != null)
        {
            while (btn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(btn.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(btn.onClick, controller.OnTutorialLaboratory);
        }

        // A cloned Laboratory button may inherit DemoButtonVisibility's hidden state
        // if the panel ever gated it; practice mode is always offered.
        tut.SetActive(true);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TutorialModeBuilder] Tutorial menu button built and wired to OnTutorialLaboratory.");
    }

    // ---- SampleScene: the guidance rig ---------------------------------------

    /// Wires Tutorial Mode into the lab scene:
    ///   • TutorialHighlighter on the runner's object, bound to the runner
    ///   • WaypointGuide bound to the runner (it has been dead since the zone-free
    ///     conversion killed the station registry it used to read)
    ///   • the beacon's arrow + disc switched to a ZTest Always material so the
    ///     marker reads THROUGH a closed cabinet door — the "see it through things"
    ///     requirement, without touching any shared bench material.
    /// Idempotent.
    [MenuItem("Tools/PharmaSynth/Build Tutorial Scene Wiring")]
    public static void BuildSceneWiring()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TutorialModeBuilder] exit Play mode first."); return; }
        var runner = Object.FindAnyObjectByType<ExperimentRunner>();
        if (runner == null)
        {
            Debug.LogError("[TutorialModeBuilder] no ExperimentRunner — open SampleScene.unity first.");
            return;
        }

        var hl = runner.GetComponent<TutorialHighlighter>();
        if (hl == null) hl = runner.gameObject.AddComponent<TutorialHighlighter>();
        hl.Bind(runner);
        EditorUtility.SetDirty(hl);

        var coach = runner.GetComponent<TutorialCoach>();
        if (coach == null) coach = runner.gameObject.AddComponent<TutorialCoach>();
        coach.Bind(runner);
        EditorUtility.SetDirty(coach);

        int beacons = 0;
        var mat = EnsureThroughWallMaterial();
        foreach (var guide in Object.FindObjectsByType<WaypointGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            guide.SetRunner(runner);
            EditorUtility.SetDirty(guide);
            foreach (var r in guide.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;      // sharedMaterial: .material instances in edit mode
                EditorUtility.SetDirty(r);
            }
            beacons++;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TutorialModeBuilder] highlighter bound; " + beacons + " waypoint guide(s) revived with a through-wall beacon.");
    }

    /// Unlit + ZTest Always + ZWrite Off: the beacon draws on top of whatever is in
    /// front of it. Its own asset, never a shared bench material — flipping ZTest on
    /// one of those would make random glassware render through walls too.
    static Material EnsureThroughWallMaterial()
    {
        const string path = "Assets/PharmaSynth/Art/Materials/TutorialBeacon.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = "TutorialBeacon" };
        mat.SetColor("_BaseColor", new Color(1f, 0.72f, 0.20f, 1f));
        mat.SetColor("_Color", new Color(1f, 0.72f, 0.20f, 1f));
        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 4000;                    // after opaque geometry
        System.IO.Directory.CreateDirectory("Assets/PharmaSynth/Art/Materials");
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // ---- coverage audit ------------------------------------------------------

    /// Builds every module's stage IN EDIT MODE and reports which steps resolve to no
    /// scene object — i.e. where Tutorial Mode would tell the player to act with
    /// nothing to point at.
    ///
    /// NOT a self-test pin, deliberately: this has to Build() each stage, which mutates
    /// the open scene, and the suite must stay side-effect-free. Run it by hand after
    /// changing a module's tasks, layout, or verb wiring. Like Reveal Stage, it leaves
    /// the LAST module's stage standing — rebuild or reopen the scene afterwards.
    [MenuItem("Tools/PharmaSynth/Audit Tutorial Targets")]
    public static void AuditTargets()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TutorialAudit] exit Play mode first."); return; }
        var builder = Object.FindAnyObjectByType<ExperimentSceneBuilder>();
        if (builder == null)
        {
            Debug.LogError("[TutorialAudit] no ExperimentSceneBuilder in the open scene — open SampleScene.unity first.");
            return;
        }

        var report = new System.Text.StringBuilder("[TutorialAudit] per-module step coverage\n");
        int totalGaps = 0, modules = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ExperimentModuleDefinition",
                     new[] { "Assets/PharmaSynth/ScriptableObjects/Experiments" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<ExperimentModuleDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.graphTasks == null || m.graphTasks.Count == 0) continue;

            modules++;
            builder.Build(m.moduleId);
            TutorialTargets.Build();
            TutorialTargets.AuditAgainst(m.graphTasks);

            int gaps = TutorialTargets.LastUnresolved.Count;
            totalGaps += gaps;
            report.Append(gaps == 0 ? "  OK   " : "  GAP  ")
                  .Append(m.moduleId)
                  .Append("  (").Append(m.graphTasks.Count).Append(" tasks");
            if (gaps > 0) report.Append(", unresolved: ").Append(string.Join(", ", TutorialTargets.LastUnresolved));
            report.Append(")\n");
        }

        report.Append(totalGaps == 0
            ? "  -> every step in every module resolves to at least one object."
            : "  -> " + totalGaps + " step(s) have NO object to point at; Tutorial Mode will guide the player nowhere.");
        if (totalGaps == 0) Debug.Log(report.ToString());
        else Debug.LogWarning(report.ToString());

        System.IO.File.WriteAllText("Logs/tutorial-target-audit.txt",
            report.ToString() + "\n(" + modules + " modules audited)\n");
        TaskTargetRegistry.Clear();
    }

    // ---- helpers -------------------------------------------------------------

    static GameObject FindInScene(string name)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            var t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindDeep(Transform t, string name)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            if (c.name == name) return c;
            var deep = FindDeep(c, name);
            if (deep != null) return deep;
        }
        return null;
    }
}
#endif
