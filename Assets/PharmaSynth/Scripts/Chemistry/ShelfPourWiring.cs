using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Pour wiring for HAND-PLACED bottles (user 2026-07-10: tipping a reagent-shelf
/// bottle showed no stream/puddle). ExperimentSceneBuilder wires runtime-spawned
/// pourables, but the 16 shelf display bottles (and batch-H cabinet stock) are
/// scene objects with LiquidPhysics only. This mirrors the builder's pourable
/// block for an existing bottle, idempotently, callable from the editor menu
/// (Tools ▸ PharmaSynth ▸ Wire Shelf Pourers) and from runtime builders.
///
/// Edit-mode note: SpillMistake/LiquidPourer self-bind in Awake/Start at play
/// time, so edit-mode wiring only has to ADD the components and set the
/// serialized fields (registry, spout); the runner param matters only for
/// runtime callers.
public static class ShelfPourWiring
{
    /// Ensure the bottle pours visibly and grades spills. Returns the number of
    /// components/objects added (0 = was already fully wired, -1 = not a liquid
    /// container).
    public static int WireBottle(GameObject bottle, ExperimentRunner runner, ReactionRegistry registry)
    {
        if (bottle == null) return -1;
        var lp = bottle.GetComponent<LiquidPhysics>();
        if (lp == null) return -1;

        int added = 0;
        if (lp.registry == null && registry != null) { lp.registry = registry; added++; }

        // Visible fill (W5.8): most ChemLab bottle prefabs have no liquid child
        // mesh, so pours in/out were invisible. Idempotent — authored _WithLiquid
        // setups and already-wired bottles are left alone.
        if (lp.mainRenderer == null || lp.mainRenderer.sharedMaterial == null
            || !lp.mainRenderer.sharedMaterial.HasProperty("_Fill"))
        {
            ExperimentSceneBuilder.EnsureLiquidVisual(bottle, lp);
            if (lp.mainRenderer != null) added++;
        }

        var pourer = bottle.GetComponent<LiquidPourer>();
        if (pourer == null) { pourer = bottle.AddComponent<LiquidPourer>(); added++; }
        if (pourer.spout == null)
        {
            // Mouth at the TOP of the bottle's real bounds (the old fixed 0.12 m
            // guess sat mid-body on tall bottles).
            var rends = bottle.GetComponentsInChildren<Renderer>(true);
            var spout = new GameObject("Spout").transform;
            spout.SetParent(bottle.transform, false);
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                spout.position = new Vector3(b.center.x, b.max.y - 0.005f, b.center.z);
            }
            else spout.localPosition = new Vector3(0f, 0.12f, 0f);
            pourer.spout = spout;
            added++;
        }

        if (bottle.GetComponent<SpillMistake>() == null)
        {
            var spill = bottle.AddComponent<SpillMistake>();
            spill.Bind(runner, lp, bottle.GetComponent<XRGrab>(), Mishandling.DisplayNameFor(bottle));
            added++;
        }

        if (bottle.GetComponent<HazardousMixReactor>() == null)
        {
            bottle.AddComponent<HazardousMixReactor>().Bind(lp, runner);   // bad-mix consequences
            added++;
        }

        // Feedback layer (W5.8): reaction/mix/overflow popups + a live contents
        // tag when the bottle already carries a ProximityLabel.
        if (bottle.GetComponent<MixFeedback>() == null)
        {
            bottle.AddComponent<MixFeedback>().Bind(lp);
            added++;
        }
        var label = bottle.GetComponent<ProximityLabel>();
        if (label != null && bottle.GetComponent<VesselStatus>() == null)
        {
            string display = lp.currentChemical != null ? lp.currentChemical.chemicalName : bottle.name;
            bottle.AddComponent<VesselStatus>().Bind(lp, label, display, 1.4f);
            added++;
        }
        return added;
    }
}
