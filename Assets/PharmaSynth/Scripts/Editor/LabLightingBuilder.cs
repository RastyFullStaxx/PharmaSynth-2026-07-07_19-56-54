#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Brightens the lab so it reads as a well-lit laboratory (user 2026-07-10: "a lab must
/// be well-lit, currently our lab-room is dim"). Quest-friendly recipe — NO extra
/// shadow-casting lights:
///   1. The 16 ceiling `Light (n)` fixture meshes get a white EMISSIVE panel material
///      (they were unlit gray boxes).
///   2. Flat ambient raised from the dark skybox gray (0.21) to a bright neutral.
///   3. A small grid of shadowless point lights (`LabLights` group, re-runnable) fills
///      the room — 6 lights, wide range, warm-white, no shadows (URP per-object cap safe).
///   4. The directional key light keeps shadows but drops a touch so it doesn't blow out.
///
/// Tools ▸ PharmaSynth ▸ Brighten Lab Lighting (run in SampleScene, edit mode, idempotent).
public static class LabLightingBuilder
{
    const string GroupName = "LabLights";
    const string MatPath = "Assets/PharmaSynth/Art/Generated/LabLightPanel.mat";

    [MenuItem("Tools/PharmaSynth/Brighten Lab Lighting")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[LabLighting] exit Play mode first."); return; }

        // 1. Emissive panel material on the ceiling fixtures.
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.SetColor("_BaseColor", Color.white);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", new Color(1f, 0.98f, 0.92f) * 2.2f);
        EditorUtility.SetDirty(mat);

        int fixtures = 0;
        var env = GameObject.Find("Environment");
        if (env == null) { Debug.LogError("[LabLighting] no Environment root"); return; }
        foreach (Transform t in env.transform)
        {
            if (t.name != "Light" && !t.name.StartsWith("Light (")) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            fixtures++;
        }

        // 2. Bright neutral flat ambient.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.48f);

        // 3. Shadowless fill lights (re-runnable: wipe + respawn the group).
        var old = GameObject.Find(GroupName);
        if (old != null) Object.DestroyImmediate(old);
        var group = new GameObject(GroupName);
        Undo.RegisterCreatedObjectUndo(group, "Brighten Lab Lighting");
        // Fixture grid spans x −3.32→2.66, z −0.72→−6.65 at y 2.64 — 6 lights cover it in 2×3.
        var spots = new List<Vector3>
        {
            new Vector3(-2.3f, 2.5f, -1.7f), new Vector3(1.7f, 2.5f, -1.7f),
            new Vector3(-2.3f, 2.5f, -3.7f), new Vector3(1.7f, 2.5f, -3.7f),
            new Vector3(-2.3f, 2.5f, -5.7f), new Vector3(1.7f, 2.5f, -5.7f),
        };
        foreach (var p in spots)
        {
            var go = new GameObject("FillLight");
            go.transform.SetParent(group.transform, false);
            go.transform.position = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 1.1f;
            l.color = new Color(1f, 0.97f, 0.92f);   // warm-white fluorescent
            l.shadows = LightShadows.None;
        }

        // 4. Keep the directional key but stop it blowing out the now-bright room.
        var dir = Object.FindObjectsByType<Light>();
        foreach (var l in dir)
            if (l.type == LightType.Directional) l.intensity = 1.4f;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(group.scene);
        Debug.Log("<color=#4CD07D>[LabLighting] " + fixtures + " emissive fixtures, ambient 0.45, "
            + spots.Count + " fill lights, key 1.4</color>");
    }
}
#endif
