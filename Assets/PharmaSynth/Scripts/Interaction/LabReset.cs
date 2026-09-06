using UnityEngine;

/// The one place a lab reset restores the APPARATUS (W5.59, user: "ensure that reset
/// completely resets apparatus, not just the reagents").
///
/// A reset used to re-home positions, put flames out, rebuild the task wiring and refill the
/// bottles — and leave every piece of glassware exactly as the last attempt left it: the
/// contents, the temperature, the residue stamp and the " — Tollen's test" role suffix on the
/// label all carried into the next run. A player who restarted mid-experiment inherited a
/// bench that looked used, because it was.
///
/// Apparatus is every vessel that is not a reagent bottle (`Raw_*` / `Reagent_*`, the vault's
/// naming rule). Each is taken back to empty, cool, unlabelled and unclaimed;
/// `LiquidPhysics.ClearContents` raises `Emptied`, so a pooled vessel gives its role back
/// through the same seam a rinse uses. The bottles then refill through the supply monitor,
/// and a solid jar gets its mound back at full. Positions, flames and consumables stay with
/// `DropRespawn.ResetAllHome`; task wiring with `ExperimentSceneBuilder.ClearBenchBindings`.
///
/// Called at the gate's Loading entry — every run path crosses it (first run, Retry,
/// Restart) — and by `ResetLabForReturn`, which rebuilds the stage without passing Loading.
public static class LabReset
{
    /// Pure, suite-pinned: is this the name of a reagent bottle rather than apparatus?
    public static bool IsReagent(string objectName)
        => !string.IsNullOrEmpty(objectName)
           && (objectName.StartsWith("Raw_") || objectName.StartsWith("Reagent_"));

    /// Restore every apparatus vessel and refill every reagent. Returns the apparatus count.
    public static int ResetApparatus()
    {
        int n = 0;
        foreach (var lp in Object.FindObjectsByType<LiquidPhysics>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lp == null || IsReagent(lp.name)) continue;
            n++;
            var last = lp.currentChemical != null ? lp.currentChemical : lp.LastChemical;
            lp.ClearContents();                                  // → Emptied → ResetRole on the binding
            if (last != null && (last.state == PhysicalState.Solid || last.state == PhysicalState.Powder))
                ExperimentSceneBuilder.EnsurePowderVisual(lp.gameObject, last, 0f);   // hide a leftover mound

            var temp = lp.GetComponent<TemperatureSim>();
            if (temp != null) temp.ResetSim();

            var clean = lp.GetComponent<CleanableVessel>();
            if (clean != null) clean.ResetResidue();

            var status = lp.GetComponent<VesselStatus>();
            if (status != null) { status.SetRoleSuffix(""); status.Refresh(); }
        }
        ReagentSupplyMonitor.RefillSourceBottles();               // bottles to 150, solid jars re-mounded
        return n;
    }
}
