#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// Stands tipped glassware back up, and re-bakes its respawn home (W5.55).
///
/// ⛔ A vessel whose RESTING pose reads as tipped pours itself forever: `LiquidPourer.Update`
/// fires on `Vector3.Angle(Vector3.up, transform.up) > pourThreshold`, so it empties every
/// drop put into it and runs its looping pour audio under everything else. W5.45 found both
/// distilling flasks at 90 degrees; the suite has pinned it since, and it came back the
/// moment a `Re-Home Scene Items (Adopt Current)` pass ran while a flask happened to be
/// lying down — adoption bakes whatever pose it finds, tipped or not.
///
/// Straightening the transform alone never sticks: `DropRespawn.ResetAllHome` puts the baked
/// rotation back at the start of every run, so the HOME has to be rewritten too. Both pins
/// live in the suite (`pour: no vessel rests beyond its own pour threshold` and
/// `pour: no pourable vessel's baked HOME is tipped`).
///
/// Idempotent: an already-upright bench is left completely alone.
public static class UprightPourables
{
    [MenuItem("Tools/PharmaSynth/Stand Tipped Glassware Up")]
    public static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Upright] exit Play mode first."); return; }

        int straightened = 0, rehomed = 0;
        string names = "";

        foreach (var pr in Object.FindObjectsByType<LiquidPourer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pr == null) continue;

            // Keep the yaw the user placed it at; only the tip is wrong.
            if (Vector3.Angle(Vector3.up, pr.transform.up) > pr.pourThreshold)
            {
                Undo.RecordObject(pr.transform, "Stand glassware up");
                Vector3 fwd = pr.transform.forward;
                fwd.y = 0f;
                pr.transform.rotation = Quaternion.LookRotation(
                    fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward, Vector3.up);
                EditorUtility.SetDirty(pr.transform);
                straightened++;
                if (names.Length < 120) names += (names.Length > 0 ? ", " : "") + pr.name;
            }

            // The baked home matters even when the live transform is fine: the next run
            // restores it.
            var dr = pr.GetComponent<DropRespawn>();
            if (dr == null) continue;
            var so = new SerializedObject(dr);
            var rot = so.FindProperty("_homeRot");
            if (rot == null) continue;
            if (Vector3.Angle(Vector3.up, rot.quaternionValue * Vector3.up) <= pr.pourThreshold) continue;
            Vector3 hf = rot.quaternionValue * Vector3.forward;
            hf.y = 0f;
            rot.quaternionValue = Quaternion.LookRotation(
                hf.sqrMagnitude > 1e-4f ? hf.normalized : Vector3.forward, Vector3.up);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dr);
            rehomed++;
        }

        if (straightened + rehomed > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("<color=#4CD07D>[Upright] " + straightened + " vessel(s) stood up"
                  + (names.Length > 0 ? " (" + names + ")" : "")
                  + ", " + rehomed + " baked home(s) straightened.</color>");
    }
}
#endif
