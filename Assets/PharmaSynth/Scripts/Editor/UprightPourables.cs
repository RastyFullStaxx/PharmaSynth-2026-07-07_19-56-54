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
    /// Ignore this much daylight. The 14 cabinet bottles all sit a uniform 2 cm proud of
    /// their shelf collider, which is how they were authored — not damage, and not something
    /// to "fix" by dragging every bottle down 2 cm.
    public const float Tolerance = 0.03f;

    /// A vessel whose visible glass is further than this from its own collider is not
    /// merely imprecise, it is unusable: you grab where the glass ISN'T. Normal glassware
    /// measures under 3.5 cm; the two distilling flasks measured 71 cm.
    public const float MeshOffsetLimit = 0.05f;

    /// ⛔ PUT THE GLASS BACK ON ITS COLLIDER (W5.56, user: "you move them floating in the
    /// air again", after re-placing them by hand twice and watching them hang anyway).
    ///
    /// Both distilling flasks are USD imports whose visible mesh, liquid and spout all sit
    /// 49 cm sideways and 51 cm above the object's own pivot and grab collider. Nothing the
    /// user does in the Scene view can fix that: dragging moves the pivot, and the glass
    /// keeps its offset, so the flask appears to float wherever it is put. It also means the
    /// player reaches for the glass and grabs nothing, because the collider is half a metre
    /// away. Every OTHER vessel in the lab measures under 3.5 cm, so this is damage in two
    /// objects, not a convention.
    ///
    /// ⛔ RE-PIVOT: the glass must NOT move. The first cut of this dragged the visual
    /// assembly onto the pivot, which teleported both flasks half a metre off the worktop the
    /// user had just placed them over — technically "aligned", and exactly the complaint
    /// again. The object's PIVOT is what everything else reads (grab, DropRespawn home, the
    /// seating probe, every anchor), so the pivot moves TO the glass and the children are
    /// compensated by the same amount, leaving the glass in world space untouched.
    ///
    /// The whole visual assembly shifts together, so the liquid stays inside the glass and
    /// the spout stays at the lip.
    static bool AlignMeshToCollider(LiquidPourer pr, out string note)
    {
        note = "";
        var col = pr.GetComponent<Collider>();
        if (col == null) return false;
        Bounds mesh = ExperimentSceneBuilder.SolidWorldBounds(pr.gameObject);
        if (mesh.size == Vector3.zero) return false;
        Vector3 delta = mesh.center - col.bounds.center;      // pivot -> glass
        if (delta.magnitude <= MeshOffsetLimit) return false;

        Undo.RecordObject(pr.transform, "Re-pivot vessel");
        pr.transform.position += delta;                       // pivot lands on the glass
        foreach (Transform child in pr.transform)
        {
            Undo.RecordObject(child, "Re-pivot vessel");
            child.position -= delta;                           // ...and the glass stays put
            EditorUtility.SetDirty(child);
        }
        EditorUtility.SetDirty(pr.transform);
        note = pr.name + " re-pivoted " + delta.magnitude.ToString("0.00") + " m onto its glass";
        return true;
    }

    /// The highest collider under a vessel, probed at the centre AND four corners of its
    /// footprint. One ray is not enough: a single ray can slip through a gap in a table's
    /// mesh collider and report the floor 30 cm lower, which would seat the vessel inside
    /// the furniture. Returns false when nothing is under it at all.
    static bool SurfaceUnder(Bounds b, out float y)
    {
        y = 0f;
        bool found = false;
        float dx = b.extents.x * 0.4f, dz = b.extents.z * 0.4f;
        var offsets = new[]
        {
            Vector2.zero, new Vector2(dx, 0f), new Vector2(-dx, 0f),
            new Vector2(0f, dz), new Vector2(0f, -dz)
        };
        foreach (var o in offsets)
        {
            var from = new Vector3(b.center.x + o.x, b.min.y + 0.02f, b.center.z + o.y);
            if (!Physics.Raycast(from, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (!found || hit.point.y > y) { y = hit.point.y; found = true; }
        }
        return found;
    }

    /// Drop a vessel until its BASE rests on the surface below, keeping the x and z the user
    /// chose, and re-bake its respawn home so a reset keeps it there.
    static bool SeatOnSurface(LiquidPourer pr, out string note)
    {
        note = "";
        Bounds b = ExperimentSceneBuilder.SolidWorldBounds(pr.gameObject);
        if (b.size == Vector3.zero) return false;
        if (!SurfaceUnder(b, out float surfaceY))
        { note = pr.name + ": nothing underneath — left where it is"; return false; }

        float gap = b.min.y - surfaceY;
        if (gap <= Tolerance && gap >= -Tolerance) return false;

        Undo.RecordObject(pr.transform, "Seat glassware");
        // Bottom-align by BOUNDS, never the pivot: these flasks pivot at their centre.
        pr.transform.position += Vector3.up * (surfaceY + 0.002f - b.min.y);
        EditorUtility.SetDirty(pr.transform);
        note = pr.name + " " + (gap * 100f).ToString("0") + " cm → seated at y "
               + surfaceY.ToString("0.000");

        var dr = pr.GetComponent<DropRespawn>();
        if (dr != null)
        {
            Undo.RecordObject(dr, "Seat glassware");
            dr.SetHome(pr.transform.position, pr.transform.rotation);
            EditorUtility.SetDirty(dr);
        }
        return true;
    }

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

        // ⭐ The glass has to be ON its collider before anything else is measurable: a
        // vessel whose mesh is half a metre from its pivot cannot be seated, because seating
        // moves the pivot and the glass keeps the offset.
        int aligned = 0; string alignNotes = "";
        foreach (var pr in Object.FindObjectsByType<LiquidPourer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pr == null || !pr.gameObject.activeInHierarchy) continue;
            if (!AlignMeshToCollider(pr, out string note)) continue;
            aligned++;
            if (alignNotes.Length < 160) alignNotes += (alignNotes.Length > 0 ? "; " : "") + note;
        }

        // ⭐ SEATING (W5.56, user: "you move them floating in the air again"). Standing a
        // vessel up is only half the job: a flask whose pivot is at its CENTRE keeps the same
        // pivot height when it rotates, so an upright flask can still hang. And a lost hand
        // placement leaves them metres off. Seat the base on whatever is underneath, keeping
        // the x and z the user chose, and re-home so the next reset cannot undo it.
        int seated = 0; string seatNotes = "";
        foreach (var pr in Object.FindObjectsByType<LiquidPourer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pr == null || !pr.gameObject.activeInHierarchy) continue;
            if (!SeatOnSurface(pr, out string note)) { if (note.Length > 0) Debug.LogWarning("[Upright] " + note); continue; }
            seated++;
            if (seatNotes.Length < 160) seatNotes += (seatNotes.Length > 0 ? "; " : "") + note;
        }

        if (straightened + rehomed + seated + aligned > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("<color=#4CD07D>[Upright] " + straightened + " vessel(s) stood up"
                  + (names.Length > 0 ? " (" + names + ")" : "")
                  + ", " + rehomed + " baked home(s) straightened, " + aligned + " mesh(es) aligned"
                  + (alignNotes.Length > 0 ? " (" + alignNotes + ")" : "")
                  + ", " + seated + " seated"
                  + (seatNotes.Length > 0 ? ": " + seatNotes : "") + ".</color>");
    }
}
#endif
