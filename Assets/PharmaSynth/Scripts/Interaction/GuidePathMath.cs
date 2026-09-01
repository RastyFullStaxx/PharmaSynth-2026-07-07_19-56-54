using System.Collections.Generic;
using UnityEngine;

/// Pure geometry for Tutorial Mode's ground path (W5.44): given the corners of a walkable
/// route, where does each flowing chevron sit and which way does it face?
///
/// Pure so the suite can pin the SHAPE of the path without a headset — that chevrons are
/// evenly spaced regardless of how unevenly the navmesh corners fall, that they face along
/// the route rather than at the destination, and that the flow runs toward the target and
/// not away from it. A path that compiles and points backwards is not something a compile
/// or a play-mode run would ever complain about.
public static class GuidePathMath
{
    /// Metres between chevrons. Close enough to read as a continuous flow at walking pace,
    /// far enough apart not to become a solid stripe.
    public const float Spacing = 0.45f;

    /// Metres per second the pattern slides toward the destination. Deliberately slower
    /// than a walk: a flow that outruns the player reads as "hurry up" rather than "this
    /// way", and this mode is explicitly untimed.
    public const float FlowSpeed = 0.7f;

    /// Hard ceiling on drawn chevrons, so a pathological route across the whole lab cannot
    /// spawn hundreds of quads.
    public const int MaxChevrons = 40;

    public struct Chevron
    {
        public Vector3 position;
        public Quaternion rotation;
        /// 0 at the player, 1 at the destination — drives the fade-in at the far end.
        public float along01;
    }

    /// Total length of a polyline. Zero for null/short input rather than throwing: an
    /// unreachable target is a normal outcome here, not an error.
    public static float Length(IReadOnlyList<Vector3> corners)
    {
        if (corners == null || corners.Count < 2) return 0f;
        float total = 0f;
        for (int i = 1; i < corners.Count; i++) total += Vector3.Distance(corners[i - 1], corners[i]);
        return total;
    }

    /// The point `distance` metres along the polyline, plus the direction of travel there.
    /// Clamped at both ends, so callers never have to bounds-check.
    public static bool Sample(IReadOnlyList<Vector3> corners, float distance,
                              out Vector3 point, out Vector3 forward)
    {
        point = default; forward = Vector3.forward;
        if (corners == null || corners.Count < 2) return false;

        if (distance <= 0f)
        {
            point = corners[0];
            var d0 = corners[1] - corners[0];
            if (d0.sqrMagnitude > 1e-6f) forward = d0.normalized;
            return true;
        }

        float walked = 0f;
        for (int i = 1; i < corners.Count; i++)
        {
            Vector3 seg = corners[i] - corners[i - 1];
            float len = seg.magnitude;
            if (len < 1e-6f) continue;
            if (walked + len >= distance)
            {
                float t = (distance - walked) / len;
                point = Vector3.Lerp(corners[i - 1], corners[i], t);
                forward = seg / len;
                return true;
            }
            walked += len;
        }

        point = corners[corners.Count - 1];
        var last = corners[corners.Count - 1] - corners[corners.Count - 2];
        if (last.sqrMagnitude > 1e-6f) forward = last.normalized;
        return true;
    }

    /// Lay chevrons along the route, offset by a scrolling phase so the pattern FLOWS.
    ///
    /// The phase subtracts from the distance rather than adding to it: the marks must
    /// travel from the player TOWARD the destination. Getting that sign wrong produces a
    /// path that visibly pulls you backwards, which is worse than no path at all — hence
    /// the suite pin.
    public static List<Chevron> Build(IReadOnlyList<Vector3> corners, float time,
                                      float spacing = Spacing, float flowSpeed = FlowSpeed)
    {
        var result = new List<Chevron>();
        float total = Length(corners);
        if (total < 0.05f) return result;

        spacing = Mathf.Max(0.05f, spacing);
        // Phase runs 0→spacing and wraps, so the marks slide forward continuously instead
        // of every chevron jumping a whole step at once.
        float phase = Mathf.Repeat(time * flowSpeed, spacing);

        for (float d = phase; d <= total && result.Count < MaxChevrons; d += spacing)
        {
            if (!Sample(corners, d, out var p, out var fwd)) break;
            result.Add(new Chevron
            {
                position = p,
                rotation = Quaternion.LookRotation(fwd, Vector3.up),
                along01 = total > 0f ? Mathf.Clamp01(d / total) : 0f,
            });
        }
        return result;
    }

    /// Which navigation cue answers "where is it" right now.
    ///
    /// The two must never draw together (design rule, W5.44): the floor path routes AROUND
    /// the benches and is what you want at a distance; the beacon reads THROUGH a cabinet
    /// door and is what you want once the object itself is the question. Showing both
    /// makes each one weaker.
    public const float NearDistance = 2f;

    public static bool ShowPath(float distanceToTarget, bool hasRoute)
        => hasRoute && distanceToTarget > NearDistance;

    public static bool ShowBeacon(float distanceToTarget, bool hasRoute)
        => !ShowPath(distanceToTarget, hasRoute);
}
