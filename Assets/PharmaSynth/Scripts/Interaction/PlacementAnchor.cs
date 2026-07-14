using UnityEngine;

/// A tiny draggable marker the user positions in the editor to tell a verb
/// exactly WHERE something happens on an imported mesh — the flame on a match
/// head, the flame on a burner mouth, the bowl of a scoopula. Bounds-based
/// guessing can't know a model's axis convention (user 2026-07-14: "can I drag
/// these to the specific parts?"), so the code reads this anchor's position and
/// falls back to a heuristic only when no anchor is present.
///
/// Convention: a child named "FlameAnchor" (Matchstick / BurnerController) or
/// "ScoopAnchor" (ScoopController). Drag it in the Scene view, then Lock My
/// Layout to bake it. The gizmo just makes it easy to see/select.
public class PlacementAnchor : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.9f);
    public float gizmoRadius = 0.006f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius * 2.2f);
    }
}
