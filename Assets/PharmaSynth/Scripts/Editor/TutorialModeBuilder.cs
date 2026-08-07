#if UNITY_EDITOR
using System.Collections.Generic;
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
        hl.SetGlowMaterials(
            EnsureGlowMaterial("TutorialGlowSource", new Color(1f, 0.66f, 0.12f, 1f)),    // amber: fetch this
            EnsureGlowMaterial("TutorialGlowTarget", new Color(0.30f, 1f, 0.45f, 1f)));   // green: put it here
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
            guide.SetMarkerScale(3.2f);                  // headset feedback: read as too small
            guide.SetPlacement(0.16f, 0.4f, 0.3f);       // clear the TOP; cage → pull in front
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

    /// Unlit + ZTest **Greater** + ZWrite Off: draws the ghost ONLY where the object is
    /// occluded, so an unobstructed bottle looks completely normal and a hidden one
    /// shows through the cabinet door. Semi-transparent so it reads as a hint, not a
    /// solid object sitting in the wall.
    /// The pulsing guidance shell. Two passes live inside PharmaSynth/GuideGlow — an
    /// additive fresnel rim where the object is visible, a flat ghost where it is
    /// hidden — so one material covers both "make it glow" and "let me see it through
    /// the cabinet". The pulse is driven by _Time in the shader, not from C#.
    static Material EnsureGlowMaterial(string name, Color colour)
    {
        string path = "Assets/PharmaSynth/Art/Materials/" + name + ".mat";
        var shader = Shader.Find("PharmaSynth/GuideGlow");
        if (shader == null)
        {
            Debug.LogError("[TutorialModeBuilder] PharmaSynth/GuideGlow not found — did PharmaGlow.shader import?");
            return null;
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            System.IO.Directory.CreateDirectory("Assets/PharmaSynth/Art/Materials");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        // Tuned against a headset capture (user 2026-08-07: "slower, larger, brighter,
        // halo wider"). The first pass was a fast, tight, dim shimmer that read as a
        // specular highlight rather than a deliberate marker.
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Intensity", 4.5f);      // was 2.2 — brighter
        mat.SetFloat("_RimPower", 1.1f);       // was 2.2 — LOWER = wider halo
        mat.SetFloat("_PulseSpeed", 1.3f);     // was 3.0 — slower, ~5 s a breath
        mat.SetFloat("_PulseMin", 0.15f);      // was 0.35 — deeper swing = bigger pulse
        mat.SetFloat("_Occluded", 0.5f);       // was 0.35 — clearer through a door
        mat.SetFloat("_Swell", 0.012f);        // 12 mm breath, so the pulse changes SIZE
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// Both guidance materials come from PharmaSynth/GuideOverlay, NOT URP's stock
    /// Unlit — that shader does not declare a `_ZTest` property, so SetInt("_ZTest",…)
    /// against it is a silent no-op and the overlay renders with ordinary depth
    /// testing (i.e. shows through nothing). The material looks correctly configured
    /// in the inspector either way, which is what makes it worth a comment.
    ///
    /// Rewrites an existing asset in place rather than returning early, so a material
    /// left over from the stock-Unlit version is repaired instead of kept.
    static Material EnsureGuideMaterial(string name, Color colour,
        UnityEngine.Rendering.CompareFunction zTest, int queue)
    {
        string path = "Assets/PharmaSynth/Art/Materials/" + name + ".mat";
        var shader = Shader.Find("PharmaSynth/GuideOverlay");
        if (shader == null)
        {
            Debug.LogError("[TutorialModeBuilder] PharmaSynth/GuideOverlay shader not found — did PharmaGuide.shader import?");
            return null;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            System.IO.Directory.CreateDirectory("Assets/PharmaSynth/Art/Materials");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_ZTest", (float)zTest);
        mat.renderQueue = queue;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// Unlit + ZTest Always + ZWrite Off: the beacon draws on top of whatever is in
    /// front of it. Its own asset, never a shared bench material — flipping ZTest on
    /// one of those would make random glassware render through walls too.
    static Material EnsureThroughWallMaterial()
        => EnsureGuideMaterial("TutorialBeacon", new Color(1f, 0.72f, 0.20f, 1f),
                               UnityEngine.Rendering.CompareFunction.Always, 4000);

    // ---- end-to-end guidance simulation --------------------------------------

    /// Walks every module's REAL task graph step by step with Tutorial Mode on, and
    /// checks the guidance actually keeps up: at every point along the progression the
    /// currently-available step must still resolve to a live object, and the clock must
    /// never tick.
    ///
    /// This is the dynamic counterpart to Audit Tutorial Targets. The audit asks "does
    /// every task have a target?" once, in aggregate; this asks "as steps complete, does
    /// the target set MOVE with them?" — a stale or empty set mid-run would leave the
    /// player staring at a lab with nothing lit, which the aggregate check cannot see.
    ///
    /// Mutates the open scene (it builds stages and starts runs) — reopen SampleScene
    /// afterwards. Deliberately a menu item, not a suite pin, for that reason.
    [MenuItem("Tools/PharmaSynth/Simulate Tutorial Guidance")]
    public static void SimulateGuidance()
    {
        if (Application.isPlaying) { Debug.LogWarning("[TutorialSim] exit Play mode first."); return; }
        var builder = Object.FindAnyObjectByType<ExperimentSceneBuilder>();
        var runner = Object.FindAnyObjectByType<ExperimentRunner>();
        var lib = AssetDatabase.LoadAssetAtPath<ExperimentLibrary>(
            "Assets/PharmaSynth/ScriptableObjects/ExperimentLibrary.asset");
        if (builder == null || runner == null || lib == null)
        {
            Debug.LogError("[TutorialSim] need ExperimentSceneBuilder + ExperimentRunner + ExperimentLibrary — open SampleScene.unity first.");
            return;
        }

        // The sim starts real runs; restore vessel contents afterwards or an edit-mode
        // pass permanently corrupts the saved scene's supplies (SimulatedRun's lesson).
        var snapshot = new List<(LiquidPhysics lp, ChemicalData chem, float ml, ChemicalData ppt, float pptMl)>();
        foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            snapshot.Add((lp, lp.currentChemical, lp.currentLiquidVolume, lp.currentPptChemical, lp.currentPptVolume));

        bool wasTutorial = TutorialSession.Active;
        TutorialSession.Active = true;
        var report = new System.Text.StringBuilder("[TutorialSim] step-by-step guidance walk\n");
        int totalBlind = 0, modules = 0;

        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ExperimentModuleDefinition",
                         new[] { "Assets/PharmaSynth/ScriptableObjects/Experiments" }))
            {
                var def = AssetDatabase.LoadAssetAtPath<ExperimentModuleDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null || def.graphTasks == null || def.graphTasks.Count == 0) continue;
                var module = lib.Get(def.moduleId);
                if (module == null) continue;

                modules++;
                builder.Build(def.moduleId);
                runner.SetModule(module);
                runner.StartExperiment();
                TutorialTargets.Build();

                var blind = new List<string>();
                int steps = 0, guided = 0;
                while (steps++ < 300)
                {
                    ExperimentTask next = null;
                    foreach (var t in runner.Graph.AvailableTasks()) { next = t; break; }
                    if (next == null) break;

                    // Would anything actually light for this step, right now?
                    var targets = TaskTargetRegistry.Targets(next.taskId);
                    bool lit = false;
                    for (int i = 0; i < targets.Count && !lit; i++)
                        if (targets[i].transform != null
                            && TutorialHighlighter.ShouldLight(targets[i], false, true)) lit = true;

                    if (lit) guided++;
                    else if (!next.autoCompleteWhenOthersDone) blind.Add(next.taskId);

                    runner.CompleteTask(next.taskId);
                }

                // The clock must not have moved: practice runs are untimed.
                bool untimed = runner.ElapsedSeconds <= 0.001f;
                totalBlind += blind.Count;
                report.Append(blind.Count == 0 && untimed ? "  OK   " : "  BAD  ")
                      .Append(def.moduleId)
                      .Append("  guided ").Append(guided).Append('/').Append(guided + blind.Count)
                      .Append(untimed ? ", untimed" : ", CLOCK RAN (" + runner.ElapsedSeconds.ToString("0.0") + "s)");
                if (blind.Count > 0) report.Append(", blind steps: ").Append(string.Join(", ", blind));
                report.Append('\n');

                runner.Abort();
            }
        }
        finally
        {
            TutorialSession.Active = wasTutorial;
            foreach (var s in snapshot)
            {
                if (s.lp == null) continue;
                s.lp.SetContents(s.chem, s.ml);
                s.lp.currentPptChemical = s.ppt;
                s.lp.currentPptVolume = s.pptMl;
            }
            TaskTargetRegistry.Clear();
        }

        report.Append(totalBlind == 0
            ? "  -> guidance kept up at every step of every module."
            : "  -> " + totalBlind + " step(s) would leave the player with nothing lit.");
        if (totalBlind == 0) Debug.Log(report.ToString()); else Debug.LogWarning(report.ToString());
        System.IO.File.WriteAllText("Logs/tutorial-guidance-sim.txt",
            report.ToString() + "\n(" + modules + " modules walked)\n");
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
