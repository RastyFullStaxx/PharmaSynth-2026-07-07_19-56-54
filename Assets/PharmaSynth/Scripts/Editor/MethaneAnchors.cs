#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Creates the draggable placement anchors the verbs read (user 2026-07-14: "can
/// I drag these to the specific parts?"). After running this, each match/burner
/// gets a "FlameAnchor" child and each scoopula/spatula a "ScoopAnchor" child,
/// dropped at a best-guess spot. SELECT it in the Hierarchy (orange gizmo in the
/// Scene view), drag it onto the exact part — match head, burner mouth, scoop
/// bowl — then run Lock My Layout to bake it. Idempotent: never moves an anchor
/// you've already positioned.
public static class MethaneAnchors
{
    [MenuItem("Tools/PharmaSynth/Add Placement Anchors (drag to fine-tune)")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Anchors] exit Play mode first."); return; }

        int flames = 0, scoops = 0;

        foreach (var m in Object.FindObjectsByType<Matchstick>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (EnsureAnchor(m.gameObject, "FlameAnchor", TipGuess(m.gameObject, positiveEnd: false))) flames++;

        foreach (var burner in Object.FindObjectsByType<BurnerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (EnsureAnchor(burner.gameObject, "FlameAnchor", BarrelTop(burner.gameObject))) flames++;

        foreach (var s in Object.FindObjectsByType<ScoopController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (EnsureAnchor(s.gameObject, "ScoopAnchor", TipGuess(s.gameObject, positiveEnd: false))) scoops++;

        // Mortars get a BowlAnchor = where the ground powder + dust appear (user
        // 2026-07-15: drag it into the bowl so the heap sits right).
        int bowls = 0;
        foreach (var g in Object.FindObjectsByType<GrindController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (EnsureAnchor(g.gameObject, "BowlAnchor", BowlGuess(g.gameObject))) bowls++;

        // Solid RECEIVERS (the hard-glass tube) get a PowderAnchor: drag it where
        // the powder should sit AND SCALE it to set the powder's size by hand
        // (user 2026-07-15). Its scale = the size at a full charge.
        int powders = 0;
        foreach (var li in Object.FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (li == null || li.itemId != "glass-tube") continue;
            if (EnsureAnchor(li.gameObject, "PowderAnchor", BowlGuess(li.gameObject), PowderScale(li.gameObject)))
                powders++;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"<color=#4CD07D>[Anchors] {flames} FlameAnchor + {scoops} ScoopAnchor + {bowls} BowlAnchor + "
                  + $"{powders} PowderAnchor placed. Select each in the Hierarchy and drag the orange marker onto the "
                  + "exact part (match head / burner mouth / scoop bowl / inside the mortar bowl). "
                  + "The PowderAnchor on the hard-glass tube also takes its SCALE — set it to the powder size you "
                  + "want. Then run Lock My Layout.</color>");
    }

    /// Add a named anchor child at a guess position/scale — but leave any existing
    /// one exactly where (and at whatever size) the user set it.
    static bool EnsureAnchor(GameObject host, string name, Vector3 worldPos, Vector3? localScale = null)
    {
        var existing = host.transform.Find(name);
        if (existing != null)
        {
            var pa0 = existing.GetComponent<PlacementAnchor>() ?? existing.gameObject.AddComponent<PlacementAnchor>();
            // Only a size-setting anchor previews its scale — a position-only anchor
            // has scale 1 and would draw a 1 m sphere over the bench (user 2026-07-15).
            pa0.previewsScale = localScale.HasValue;
            EditorUtility.SetDirty(existing.gameObject);
            return false;   // respect the user's placement + hand-set size
        }
        var go = new GameObject(name);
        go.transform.SetParent(host.transform, true);
        go.transform.position = worldPos;
        go.transform.localRotation = Quaternion.identity;
        if (localScale.HasValue) go.transform.localScale = localScale.Value;
        var pa = go.AddComponent<PlacementAnchor>();
        pa.previewsScale = localScale.HasValue;
        EditorUtility.SetDirty(host);
        return true;
    }

    /// A sensible starting size for a powder charge inside this vessel: a squat
    /// mound about half the bore. The user then scales the anchor to taste.
    static Vector3 PowderScale(GameObject go)
    {
        var lb = ExperimentSceneBuilder.LocalMeshBounds(go.transform, "Powder", "Liquid", "CollectedGas");
        int ax = ExperimentSceneBuilder.LongestAxis(lb.size);
        float bore = ExperimentSceneBuilder.BoreOf(lb.size, ax);
        float w = Mathf.Max(1e-4f, bore * 0.5f);
        return new Vector3(w, Mathf.Max(1e-4f, lb.size[ax] * 0.12f), w);
    }

    /// Far end of the longest local axis — the tip guess for a match head / scoop bowl.
    static Vector3 TipGuess(GameObject go, bool positiveEnd)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return go.transform.position;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 axis = Matchstick.LongestLocalAxis(go.transform, b, out float half);
        return b.center + axis * (half * (positiveEnd ? 1f : -1f));
    }

    /// Interior of the mortar bowl — the middle, a little above centre.
    static Vector3 BowlGuess(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return go.transform.position;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return new Vector3(b.center.x, b.center.y + b.size.y * 0.1f, b.center.z);
    }

    /// Top-centre of the tallest renderer (the burner barrel mouth).
    static Vector3 BarrelTop(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return go.transform.position + Vector3.up * 0.12f;
        Renderer top = rs[0];
        for (int i = 1; i < rs.Length; i++) if (rs[i].bounds.max.y > top.bounds.max.y) top = rs[i];
        var tb = top.bounds;
        return new Vector3(tb.center.x, tb.max.y + 0.01f, tb.center.z);
    }
}
#endif
