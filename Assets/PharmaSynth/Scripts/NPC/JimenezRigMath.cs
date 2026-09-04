using UnityEngine;

/// Pure rules for correcting an auto-rigger's weight bleed (suite-pinned).
///
/// Dr. Jimenez is a Tripo auto-rig: ONE SkinnedMeshRenderer over 41 bones, with the lab coat
/// baked into the body mesh. Nothing can be unparented — the coat follows his arm because
/// coat vertices carry weight on the arm bones. Auto-riggers assign weights by proximity, so
/// a coat panel hanging near the elbow picks up elbow weight even though a real coat would
/// hang from the shoulders.
///
/// The correction is geometric: a vertex that carries arm weight while sitting well OUTSIDE
/// the arm is bleed, and its weight belongs on the spine.
public static class JimenezRigMath
{
    /// ⛔ The sleeve radius must be expressed in the MESH's own units, not in metres.
    /// This mesh is 0.5 units tall and the prefab scales it to a 1.75 m man, so a "0.11 m"
    /// radius was HALF THE CHARACTER'S WIDTH and classified the entire coat as sleeve —
    /// the first run freed 43 vertices out of 6992 and changed nothing visible.
    ///
    /// Expressed as a fraction of the model's height it is scale-proof: a re-export at any
    /// size still separates a sleeve from a coat panel. 7% of height ≈ a 12 cm sleeve on a
    /// 1.75 m man, which is a loose lab coat.
    public const float SleeveRadiusFraction = 0.07f;

    public static float SleeveRadiusFor(float meshHeight)
        => Mathf.Max(1e-4f, meshHeight) * SleeveRadiusFraction;

    /// Bones that swing the arm.
    ///
    /// ⛔ Two deliberate exclusions, both of which shredded the model when they were in:
    ///
    /// The CLAVICLE, because the shoulder of a coat really does follow the clavicle, and
    /// freeing it would detach the yoke.
    ///
    /// The HAND, because a lab coat has no geometry past the wrist, so there is nothing there
    /// to free — and this rig has NO finger bones, which makes `L_Hand` a LEAF. A leaf bone
    /// has no segment, so its "distance to the bone" collapses to distance from the wrist
    /// joint, every fingertip measures as far away, and the whole hand is reclassified as
    /// coat and handed to the spine. That renders as fingers stretched into ribbons hanging
    /// off the wrists (verified 2026-09-05). Nothing is lost by skipping it.
    public static bool IsArmBone(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return false;
        if (!boneName.StartsWith("L_") && !boneName.StartsWith("R_")) return false;
        return boneName.Contains("Upperarm") || boneName.Contains("Forearm");
    }

    /// A twist bone is not a limb of its own — it is a sub-bone that spins WITHIN its parent
    /// limb, so it must be measured against the parent's segment, not its own.
    ///
    /// ⛔ Measuring a twist bone against itself is what tore the sleeve off (2026-09-05).
    /// `L_UpperarmTwist02` is a LEAF, so its "segment" is a single point at the elbow; sleeve
    /// vertices weighted to it all measured as far away, were called coat, and were handed to
    /// the spine — so the sleeve stayed behind while the arm rose, stretching it into ribbons
    /// hanging from the wrists. `L_UpperarmTwist01` is barely better: its segment is the 0.038
    /// stub to Twist02. Judged against the whole shoulder-to-elbow limb instead, both are
    /// correct, and no threshold has to be guessed.
    public static bool IsTwistBone(string boneName)
        => !string.IsNullOrEmpty(boneName) && boneName.Contains("Twist");

    /// Which child bone is a bone's segment end: the FARTHEST one, never the nearest.
    ///
    /// ⛔ This rig co-locates every twist bone with its parent — `L_UpperarmTwist01` sits at
    /// EXACTLY the shoulder joint. Taking the nearest child therefore gave `L_Upperarm` a
    /// segment of length 0.000, so "distance along the arm" silently became "distance from
    /// the shoulder joint" and the sleeve near the elbow read as coat. Taking the farthest
    /// child picks Upperarm→Forearm and Forearm→Hand, which are the real limbs.
    public static bool IsBetterSegmentEnd(float candidateDistance, float bestSoFar)
        => candidateDistance > bestSoFar;

    /// Bones the freed weight may be handed to, best first.
    public static bool IsTorsoBone(string boneName)
        => boneName == "Spine02" || boneName == "Spine01" || boneName == "Waist";

    /// Is this arm weight bleed rather than sleeve?
    public static bool IsBleed(float distanceToBone, float sleeveRadius)
        => distanceToBone > sleeveRadius;

    /// Distance from a point to a bone SEGMENT (bone head to its child), not just to the
    /// joint — an upper arm is a long limb and measuring to the shoulder joint alone would
    /// call the whole forearm "far away".
    public static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-8f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return Vector3.Distance(p, a + ab * t);
    }

    /// Move every gram of weight sitting on `fromBone` onto `toBone`, keeping the four
    /// influences summing to 1. Returns the amount transferred (0 when there was none).
    ///
    /// Written as a value-in/value-out helper so the suite can prove the invariant that
    /// matters: a re-weighted vertex is still normalised, or the mesh deforms at the wrong
    /// scale and every fix looks like a modelling error.
    public static float Redistribute(ref BoneWeight w, int fromBone, int toBone)
    {
        float moved = 0f;
        if (w.boneIndex0 == fromBone) { moved += w.weight0; w.weight0 = 0f; }
        if (w.boneIndex1 == fromBone) { moved += w.weight1; w.weight1 = 0f; }
        if (w.boneIndex2 == fromBone) { moved += w.weight2; w.weight2 = 0f; }
        if (w.boneIndex3 == fromBone) { moved += w.weight3; w.weight3 = 0f; }
        if (moved <= 0f) return 0f;

        // Add to the target if it is already an influence; otherwise take the emptiest slot.
        if (w.boneIndex0 == toBone) w.weight0 += moved;
        else if (w.boneIndex1 == toBone) w.weight1 += moved;
        else if (w.boneIndex2 == toBone) w.weight2 += moved;
        else if (w.boneIndex3 == toBone) w.weight3 += moved;
        else
        {
            if (w.weight0 <= 0f) { w.boneIndex0 = toBone; w.weight0 = moved; }
            else if (w.weight1 <= 0f) { w.boneIndex1 = toBone; w.weight1 = moved; }
            else if (w.weight2 <= 0f) { w.boneIndex2 = toBone; w.weight2 = moved; }
            else { w.boneIndex3 = toBone; w.weight3 = moved; }
        }
        Normalise(ref w);
        return moved;
    }

    /// Unity requires the four influences to sum to 1.
    public static void Normalise(ref BoneWeight w)
    {
        float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
        if (sum <= 1e-6f) { w.weight0 = 1f; w.weight1 = w.weight2 = w.weight3 = 0f; return; }
        w.weight0 /= sum; w.weight1 /= sum; w.weight2 /= sum; w.weight3 /= sum;
    }

    public static float Sum(BoneWeight w) => w.weight0 + w.weight1 + w.weight2 + w.weight3;
}
