using UnityEngine;

/// Pure geometry for the center-table overhead shelf (W5.10): the Table_1 mesh
/// has two support rails (front z=-3.15, back z=-3.50) whose tops sit at y≈1.55;
/// the player wanted them turned into a real equipment shelf. This lays out N
/// flush platform tiles that bridge both rails and cover the full rail width, so
/// props can be placed anywhere along the row. Values match the plank the user
/// hand-placed (top at 1.563, thickness 0.03), extended to full width. Kept
/// plain so the suite pins coverage + on-rail height.
public static class WorkspaceShelfMath
{
    public const float TileCenterY = 1.548f;   // matches the user's plank centre
    public const float Thickness = 0.03f;
    public const float ZCenter = -3.32f;        // between the two rails
    public const float Depth = 0.34f;           // bridges z -3.49 .. -3.15
    public const float XMin = -1.40f, XMax = 1.40f;
    public const float Gap = 0.02f;             // hairline seam between tiles

    /// Top surface where equipment rests.
    public static float TopY => TileCenterY + Thickness * 0.5f;   // 1.563

    static float CellWidth(int count) => (XMax - XMin) / Mathf.Max(1, count);

    /// Centre of tile i of `count`, evenly dividing [XMin, XMax].
    public static Vector3 TileCenter(int i, int count)
        => new Vector3(XMin + (i + 0.5f) * CellWidth(count), TileCenterY, ZCenter);

    /// Tile box size (cell width minus the seam gap).
    public static Vector3 TileSize(int count)
        => new Vector3(Mathf.Max(0.05f, CellWidth(count) - Gap), Thickness, Depth);

    /// A subtle back-lip strip so tall items don't roll off the far edge.
    public static Vector3 LipCenter => new Vector3(0f, TileCenterY + 0.02f, ZCenter - Depth * 0.5f + 0.01f);
    public static Vector3 LipSize => new Vector3(XMax - XMin, 0.05f, 0.015f);
}
