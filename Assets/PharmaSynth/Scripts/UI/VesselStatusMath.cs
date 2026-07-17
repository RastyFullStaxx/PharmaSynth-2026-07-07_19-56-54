using UnityEngine;

/// Pure format functions for the live-status feedback layer (W5.8): vessel
/// name tags that show contents/volume, hover-card "Now:" lines, and station
/// billboards that show temperature / sim progress. Kept plain so the suite
/// pins every format. All output is TMP-safe ASCII except the em-dash, which
/// the dialogue system already uses everywhere (LiberationSans has it); the
/// degree glyph is deliberately avoided ("62 C", not "62°C").
public static class VesselStatusMath
{
    /// "Beaker — 120 ml Ethanol" / "Beaker — empty". A reagent bottle whose
    /// display name IS the chemical drops the redundant suffix ("Ethanol — 120 ml").
    public static string Compose(string displayName, string chemName, float ml)
    {
        string name = string.IsNullOrEmpty(displayName) ? "Vessel" : displayName;
        if (ml <= 1f) return name + " — empty";
        string chem = string.IsNullOrEmpty(chemName) ? "liquid" : chemName;
        if (chem == name) return name + " — " + Mathf.RoundToInt(ml) + " ml";
        return name + " — " + Mathf.RoundToInt(ml) + " ml " + chem;
    }

    /// Mixed-contents name tag (user 2026-07-17: "clear text of the current
    /// elements in this tube and their proportions"): the ledger story IS the
    /// proportions — "Test Tube 3 — Ethanol 1 ml + Distilled Water 10 ml".
    public static string ComposeMixed(string displayName, string ledgerSummary)
    {
        string name = string.IsNullOrEmpty(displayName) ? "Vessel" : displayName;
        if (string.IsNullOrEmpty(ledgerSummary)) return name + " — empty";
        return name + " — " + ledgerSummary;
    }

    /// Hover-card live suffix: "Now: 120 ml Ethanol" (+ "Mixed from: …" when
    /// the vessel holds more than one story entry) / "Now: empty".
    public static string HoverLine(string chemName, float ml, string ledgerSummary, int ledgerCount)
    {
        if (ml <= 1f) return "Now: empty";
        string chem = string.IsNullOrEmpty(chemName) ? "liquid" : chemName;
        string line = "Now: " + Mathf.RoundToInt(ml) + " ml " + chem;
        if (ledgerCount > 1 && !string.IsNullOrEmpty(ledgerSummary))
            line += "\nMixed from: " + ledgerSummary;
        return line;
    }

    /// Heat-station billboard: "4. Heat the mix\n62 C -> 150 C".
    public static string HeatLine(string baseLabel, float currentC, float targetC)
        => baseLabel + "\n" + Mathf.RoundToInt(currentC) + " C -> " + Mathf.RoundToInt(targetC) + " C";

    /// Generic sim-progress billboard: "5. Filter\nFiltering 40%".
    public static string ProgressLine(string baseLabel, string verb, float frac01)
        => baseLabel + "\n" + verb + " " + Mathf.RoundToInt(Mathf.Clamp01(frac01) * 100f) + "%";
}
