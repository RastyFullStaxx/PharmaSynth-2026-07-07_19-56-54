#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// Reagent-label compositor (§3, client style pick: MODERN). For every
/// Reagent_* bottle on the shelf: renders LabelBase_Modern + the chemical's
/// name (crisp TMP text — never AI typography) to a PNG, builds a material,
/// and mounts a label quad on the bottle facing the aisle.
/// Tools ▸ PharmaSynth ▸ Generate Reagent Labels — idempotent, re-run anytime.
public static class LabelForge
{
    const string BasePath = "Assets/PharmaSynth/Art/Generated/Labels/LabelBase_Modern.png";
    const string OutDir = "Assets/PharmaSynth/Art/Generated/Labels/Composited";
    const int W = 512, H = 683;

    [MenuItem("Tools/PharmaSynth/Generate Reagent Labels")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[LabelForge] exit Play mode first."); return; }
        var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BasePath);
        if (baseTex == null) { Debug.LogError("[LabelForge] missing base " + BasePath); return; }
        Directory.CreateDirectory(OutDir);

        var shelf = GameObject.Find("ReagentShelf");
        if (shelf == null) { Debug.LogError("[LabelForge] no ReagentShelf"); return; }

        int made = 0;
        foreach (Transform bottle in shelf.transform)
        {
            var lp = bottle.GetComponent<LiquidPhysics>();
            string chem = lp != null && lp.currentChemical != null ? lp.currentChemical.chemicalName : null;
            if (string.IsNullOrEmpty(chem)) continue;

            string safe = chem.Replace(" ", "").Replace("%", "").Replace("/", "-");
            string pngPath = OutDir + "/Label_" + safe + ".png";
            RenderLabel(baseTex, chem, pngPath);

            string matPath = OutDir + "/Label_" + safe + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);

            MountQuad(bottle, mat);
            made++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("<color=#4CD07D>[LabelForge] composited + mounted " + made + " labels</color>");
    }

    /// Render base + centred black name text via an off-scene ortho camera.
    static void RenderLabel(Texture2D baseTex, string chem, string pngPath)
    {
        var root = new GameObject("~LabelForge");
        try
        {
            var far = new Vector3(1000f, 1000f, 1000f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(root.transform);
            quad.transform.position = far;
            quad.transform.localScale = new Vector3(0.75f, 1f, 1f);
            var unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { mainTexture = baseTex };
            quad.GetComponent<MeshRenderer>().sharedMaterial = unlit;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform);
            // Quad faces -Z, camera sits at z-1 looking +Z; text floats just in front.
            textGo.transform.position = far + new Vector3(0f, -0.06f, -0.01f);
            textGo.transform.rotation = Quaternion.identity;
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.5f);
            tmp.text = chem;
            tmp.color = Color.black;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.2f; tmp.fontSizeMax = 1.6f;
            tmp.ForceMeshUpdate();

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(root.transform);
            camGo.transform.position = far + new Vector3(0f, 0f, -1f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 0.5f;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;

            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(unlit);
            AssetDatabase.ImportAsset(pngPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// Label quad proud of the bottle surface, facing the room aisle.
    static void MountQuad(Transform bottle, Material mat)
    {
        var old = bottle.Find("NameLabel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var rs = bottle.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);

        Vector3 outward = new Vector3(0.2f, 0f, -2.5f) - bottle.position;   // toward the room
        outward.y = 0f; outward = outward.normalized;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "NameLabel";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(bottle, true);
        float w = Mathf.Min(b.size.x, b.size.z) * 0.8f;
        quad.transform.localScale = Vector3.one;   // reset, then set world size via lossy compensation
        var ls = quad.transform.lossyScale;
        quad.transform.localScale = new Vector3(w / Mathf.Max(ls.x, 1e-4f), (b.size.y * 0.5f) / Mathf.Max(ls.y, 1e-4f), 1f);
        quad.transform.position = b.center + outward * (Mathf.Max(b.extents.x, b.extents.z) + 0.004f) - new Vector3(0f, b.size.y * 0.05f, 0f);
        quad.transform.rotation = Quaternion.LookRotation(-outward);        // Quad face (-Z) toward the room
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
#endif
