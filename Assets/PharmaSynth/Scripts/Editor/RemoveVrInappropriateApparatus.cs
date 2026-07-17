#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Deletes the SCENE INSTANCES of apparatus that the manuscript lists but that
/// carry no meaningful VR interaction — pure bench scaffolding and passive
/// instruments the game already abstracts (user 2026-07-17: "they should be
/// removed even from the table, but not from the folders just in case").
///
/// ⛔ DELIBERATE, DOCUMENTED EXCEPTION to the "ALL tools always present" client
/// rule. These six are NOT a decluttering of usable tools — each is either a
/// support rig the zone-free heat model made unnecessary or an instrument whose
/// reading the game shows on-screen. The PREFAB ASSETS stay in the project, so a
/// future experiment that genuinely needs one can re-place it. Do NOT let
/// "Restore All Bench Items" or an all-tools audit re-add them — they are
/// removed on purpose. Justifications live in experiments-reference.md.
public static class RemoveVrInappropriateApparatus
{
    // itemId -> why it isn't a VR interaction (also written into the docs).
    static readonly (string itemId, string why)[] Removed =
    {
        ("kit-retortstand",  "support scaffold — VR heats zone-free (bring the flask to the bath), no clamp-rig to assemble"),
        ("kit-utilityclamp", "the iron/S-clamp that holds glassware on the stand — same scaffold, no VR interaction"),
        ("kit-aspirator",    "vacuum suction for filtration/transfer — the game moves liquid by pour/decant, never suction"),
        ("kit-condenser",    "distillation cooling — distillation is an abstracted heat+collect sim, no condenser assembly"),
        ("kit-thermometer",  "temperature is shown live by the floating ProcessReadout / water-bath label, so the physical device is decorative"),
    };

    [MenuItem("Tools/PharmaSynth/Remove VR-Inappropriate Apparatus")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[RemoveApparatus] exit Play mode first."); return; }

        var ids = new HashSet<string>(Removed.Select(r => r.itemId));
        var kill = new List<GameObject>();
        foreach (var li in Object.FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (li != null && ids.Contains(li.itemId)) kill.Add(li.gameObject);

        foreach (var go in kill)
        {
            // Unpack a prefab instance child so DestroyImmediate can remove just it.
            Undo.DestroyObjectImmediate(go);
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"<color=#4CD07D>[RemoveApparatus] removed {kill.Count} scene instance(s) "
                  + "(prefabs kept in the project). Idempotent — safe to re-run.</color>\n  "
                  + string.Join("\n  ", Removed.Select(r => r.itemId + " — " + r.why)));
    }
}
#endif
