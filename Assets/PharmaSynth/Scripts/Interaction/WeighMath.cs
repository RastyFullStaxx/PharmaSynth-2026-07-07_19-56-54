using UnityEngine;

/// Pure weighing rules (W5.8): the balance pan auto-tares the vessel, so the
/// display reads the CONTENTS in grams (1 g/ml proxy — real densities are out
/// of scope per the client's record-only-yield ruling). The suite pins these.
public static class WeighMath
{
    /// Grams shown for a vessel holding `liquidMl` (+`solidG` loose solids).
    public static float MassOf(float liquidMl, float solidG = 0f)
        => Mathf.Max(0f, liquidMl) + Mathf.Max(0f, solidG);

    /// Close enough to the target measure (default ±10% with a 1 g floor).
    public static bool WithinTolerance(float massG, float targetG, float tolFrac = 0.1f)
        => Mathf.Abs(massG - targetG) <= Mathf.Max(1f, targetG * Mathf.Max(0f, tolFrac));

    /// The load must REST on the pan for a beat (no drive-by completions).
    public static bool PanSettled(float secondsOnPan, float minSeconds = 0.75f)
        => secondsOnPan >= minSeconds;

    /// The weigh step is satisfied: something correct rests settled on the pan.
    /// Chemical mode (requiredChemical set): the vessel must hold enough of it.
    /// Item mode (requiredItemId set): the named tool/prop must sit on the pan.
    /// Open mode: any settled load counts.
    public static bool Satisfied(bool settled, string requiredChemical, string occupantChemical, float occupantMl, float requiredMl,
                                 string requiredItemId, string occupantItemId)
    {
        if (!settled) return false;
        if (!string.IsNullOrEmpty(requiredChemical))
            return occupantChemical == requiredChemical && occupantMl >= requiredMl * 0.9f;
        if (!string.IsNullOrEmpty(requiredItemId))
            return occupantItemId == requiredItemId;
        return true;
    }
}
