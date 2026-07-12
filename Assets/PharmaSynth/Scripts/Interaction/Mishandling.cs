using System.Collections.Generic;
using UnityEngine;

/// Pure rules for mishandling penalties (§2: spill & breakage, user request
/// 2026-07-09): which apparatus is fragile, when an impact shatters it, and
/// when an un-held bottle counts as spilling. Kept plain-C# so the self-tests
/// pin the policy.
public static class Mishandling
{
    /// THIN GLASS that shatters when dropped — and nothing else (user 2026-07-12:
    /// "some equipment breaks even when it's not glassware to begin with").
    /// Sturdy solid glass (rods, funnels), droppers and porcelain (dish/crucible)
    /// were delisted in W5.8: the rod is now the STIR tool, and shattering a tool
    /// mid-verb is punitive; porcelain survives a bench drop in reality. Metal,
    /// wood and plastic tools never break.
    private static readonly HashSet<string> Breakables = new HashSet<string>
    {
        "Beaker_100mL", "Beaker_100mL_WithLiquid",
        "Beaker_500mL", "Beaker_500mL_WithLiquid",
        "ErlenmeyerFlask_400mL", "ErlenmeyerFlask_400mL_WithLiquid",
        "GraduatedCylinder_50mL", "GraduatedCylinder_50mL_WithLiquid",
        "TestTube", "TestTube_WithLiquid",
        "Vial", "Vial_Brown", "Vial_Brown_WithLabel", "Vial_WithLabel",
        "WatchGlass",
    };

    /// Delisted glass/ceramic (W5.8): robust in the hand, but still CLINKS like
    /// glass on impact instead of falling through to the wooden knock.
    private static readonly HashSet<string> CeramicOrSolidGlass = new HashSet<string>
    {
        "GlassRod", "Funnel", "Dropper", "EvaporatingDish", "Crucible",
    };

    public static bool IsBreakable(string prefabName) => Breakables.Contains(prefabName ?? "");
    public static IEnumerable<string> BreakableNames => Breakables;

    /// An impact at or above this speed shatters glass. 4.5 m/s ≈ a free fall
    /// of ~1.0 m onto a hard surface — a real drop from bench/shelf height
    /// breaks, but carrying an item and bumping a wall or a neighbouring bottle
    /// (a slow scrape, well under this) never does (raised 4.0→4.5 in W5.8 with
    /// the settle-freeze pass: "generally less sensitive"). Held items are
    /// additionally immune in BreakableGlassware regardless of speed.
    public const float DefaultBreakSpeed = 4.5f;

    public static bool ShouldBreak(float impactSpeed, float breakSpeed = DefaultBreakSpeed)
        => impactSpeed >= breakSpeed;

    /// Metal apparatus — everything else non-glass lands as a dull wooden knock.
    private static readonly HashSet<string> MetalItems = new HashSet<string>
    {
        "CrucibleTongs", "Forceps", "Scoopula", "Spatula", "IronRing",
        "Tripod", "RetortStand", "WireGauze", "BunsenBurner", "TestTubeHolder_Metal", "Balance",
    };

    /// SoundBank key for a drop/impact clatter, by material.
    public static string DropSoundKey(string prefabName)
    {
        if (IsBreakable(prefabName) || CeramicOrSolidGlass.Contains(prefabName ?? "")) return "glass-clink";
        if (MetalItems.Contains(prefabName ?? "")) return "drop-metal";
        return "drop-wood";
    }

    /// SoundBank key for a fired reaction's observable outcome.
    public static string SfxForOutcome(ReactionOutcome outcome)
    {
        switch (outcome)
        {
            case ReactionOutcome.Fizzing:
            case ReactionOutcome.GasEvolved:
                return "reaction-fizz";
            case ReactionOutcome.None:
                return "";                       // negative test: nothing to hear
            default:
                return "mixture-complete";       // colour change / precipitate / odour cue
        }
    }

    /// A reagent bottle is SPILLING when nobody holds it, it still has liquid,
    /// and it lies tipped past the threshold (LiquidPourer drains it; this
    /// decides whether that drain is a graded mishandling event).
    public static bool IsSpilling(float tiltDegrees, bool held, float liquidMl, float tiltThreshold = 60f)
        => !held && liquidMl > 0.5f && tiltDegrees > tiltThreshold;
}
