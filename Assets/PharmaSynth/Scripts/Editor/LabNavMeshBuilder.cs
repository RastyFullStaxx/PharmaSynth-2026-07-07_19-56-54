#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Bakes the walkable surface Tutorial Mode's ground path routes on (W5.44).
///
/// Without this there is no NavMesh in the project at all, and `GuidePath` silently draws
/// nothing — the floor arrows would look "not implemented" rather than "not baked", which
/// is exactly the sort of silence this codebase has been bitten by before.
///
/// ⚠ **The bake goes STALE when furniture moves**, precisely like the lightmap bake — the
/// benches ARE the obstacles it routes around. Re-run this in the same breath as
/// `Build Lab Probes` → `Run Lab Lighting Bake` after any `Select Movable Furniture`
/// session, or the arrows will confidently route through a bench that has since moved.
public static class LabNavMeshBuilder
{
    const string HostName = "LabNavMesh";
    const string AssetDir = "Assets/Scenes";
    const string AssetPath = AssetDir + "/LabNavMesh.asset";

    [MenuItem("Tools/PharmaSynth/Build Lab NavMesh (ground path routing)")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[LabNavMesh] exit Play mode first."); return; }

        var host = GameObject.Find(HostName);
        if (host == null)
        {
            host = new GameObject(HostName);
            Undo.RegisterCreatedObjectUndo(host, "Build Lab NavMesh");
        }

        var surface = host.GetComponent<NavMeshSurface>();
        if (surface == null) surface = host.AddComponent<NavMeshSurface>();

        // Whole scene, RENDER geometry — the latter is already NavMeshSurface's default, so
        // it is left alone rather than restated. Render meshes (not colliders) because the
        // lab's floor and furniture are ordinary meshes, and collecting by collider would
        // pull in the trigger volumes we must not walk on.
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;

        // A player-sized agent. The radius matters most: too small and the path hugs bench
        // corners the player's body cannot actually round.
        surface.agentTypeID = 0;          // the project's default Humanoid agent
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.08f;        // finer than default — this is one room, not a level

        surface.BuildNavMesh();

        var data = surface.navMeshData;
        if (data == null)
        {
            Debug.LogError("[LabNavMesh] the bake produced NO NavMeshData — the ground path "
                           + "will draw nothing. Check that the lab floor has a render mesh.");
            return;
        }

        // ⛔ PERSIST THE DATA AS ITS OWN ASSET, or the whole scene turns BINARY.
        //
        // BuildNavMesh leaves NavMeshData as an in-memory object owned by the surface, so
        // saving the scene serialises it INTO the scene — and NavMeshData cannot be written
        // as YAML. Unity then silently ignores ForceText and rewrites SampleScene.unity as
        // binary: 5.08 MB of readable YAML became 1.88 MB of bytes, git diffs became "Bin",
        // and the vault's scene generator parsed 0 objects out of it. Nothing warns you.
        System.IO.Directory.CreateDirectory(AssetDir);
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(AssetPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(data, AssetPath);
        }
        else
        {
            // Overwrite in place so the surface's reference (and its guid) survive.
            EditorUtility.CopySerialized(data, existing);
            surface.navMeshData = existing;
            data = existing;
        }
        AssetDatabase.SaveAssets();

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("<color=#4CD07D>[LabNavMesh] baked</color> — bounds "
                  + data.sourceBounds.size.ToString("0.0") + ", data at " + AssetPath
                  + ". ⚠ Re-run this after moving furniture, alongside the lighting bake.");
    }
}
#endif
