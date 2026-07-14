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

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"<color=#4CD07D>[Anchors] {flames} FlameAnchor(s) + {scoops} ScoopAnchor(s) placed. "
                  + "Select each in the Hierarchy, drag the orange marker onto the exact part "
                  + "(match head / burner mouth / scoop bowl), then run Lock My Layout.</color>");
    }

    /// Add a named anchor child at a guess position — but leave any existing one
    /// exactly where the user put it.
    static bool EnsureAnchor(GameObject host, string name, Vector3 worldPos)
    {
        var existing = host.transform.Find(name);
        if (existing != null)
        {
            if (existing.GetComponent<PlacementAnchor>() == null) existing.gameObject.AddComponent<PlacementAnchor>();
            return false;   // respect the user's placement
        }
        var go = new GameObject(name);
        go.transform.SetParent(host.transform, true);
        go.transform.position = worldPos;
        go.AddComponent<PlacementAnchor>();
        EditorUtility.SetDirty(host);
        return true;
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
