#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Builds proper equipment-shelf platforms on the center-table overhead gantry
/// (user 2026-07-12: "continue adding platforms to these rows... perfect it. I'm
/// planning to transfer some equipment there"). The user had hand-placed one
/// half-width plank (a duplicated cabinet shelf); this replaces it with a clean,
/// full-width set of flush tiles that bridge both rails, each with a collider so
/// props rest on them and a lab-grey board material so they read as built-in.
/// Idempotent + re-runnable. Geometry lives in the pure WorkspaceShelfMath.
public static class WorkspaceShelfBuilder
{
    const string RootName = "WorkspaceShelf";
    const int TileCount = 3;

    [MenuItem("Tools/PharmaSynth/Build Workspace Shelf")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[WorkspaceShelf] exit Play mode first."); return; }

        // 1) Remove the user's ad-hoc half-plank (a duplicated cabinet shelf that
        // landed over the table) and any previous WorkspaceShelf.
        int removed = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            bool adHocPlank = t.name == "Shelf_1.3 (1)" && t.position.y > 1.4f && t.position.z < -2.5f;
            if (adHocPlank) { Object.DestroyImmediate(t.gameObject); removed++; }
        }
        var existing = GameObject.Find("Environment/" + RootName) ?? GameObject.Find(RootName);
        if (existing != null) { Object.DestroyImmediate(existing); removed++; }

        // 2) Root under Environment (falls back to scene root).
        var env = GameObject.Find("Environment");
        var root = new GameObject(RootName);
        if (env != null) root.transform.SetParent(env.transform, true);

        var mat = ShelfMaterial();

        // 3) Full-width flush tiles bridging both rails.
        for (int i = 0; i < TileCount; i++)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "ShelfTile_" + i;
            tile.transform.SetParent(root.transform, true);
            tile.transform.position = WorkspaceShelfMath.TileCenter(i, TileCount);
            tile.transform.localScale = WorkspaceShelfMath.TileSize(TileCount);
            tile.GetComponent<Renderer>().sharedMaterial = mat;
            // Keep the BoxCollider the primitive ships with (props seat on it).
        }

        // 4) Subtle back-lip so tall items don't roll off the far edge.
        var lip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lip.name = "ShelfBackLip";
        lip.transform.SetParent(root.transform, true);
        lip.transform.position = WorkspaceShelfMath.LipCenter;
        lip.transform.localScale = WorkspaceShelfMath.LipSize;
        lip.GetComponent<Renderer>().sharedMaterial = mat;

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[WorkspaceShelf] built {TileCount} tiles + back-lip on the gantry (top y={WorkspaceShelfMath.TopY:F3}); " +
                  $"cleared {removed} old object(s). Equipment can now be placed on the shelf.");
    }

    /// Match the bench: reuse Table_1's board material, else a neutral lab grey.
    static Material ShelfMaterial()
    {
        var table = GameObject.Find("Table_1") ?? GameObject.Find("Environment/Table_1");
        if (table != null)
        {
            var r = table.GetComponentInChildren<Renderer>();
            if (r != null && r.sharedMaterial != null) return r.sharedMaterial;
        }
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(sh) { name = "WorkspaceShelfBoard" };
        m.color = new Color(0.30f, 0.33f, 0.40f);   // dark bench grey-blue
        return m;
    }
}
#endif
