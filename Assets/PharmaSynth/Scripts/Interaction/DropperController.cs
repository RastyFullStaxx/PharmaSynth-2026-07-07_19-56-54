using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Pure rules for the dropper/pipette verb (user 2026-07-16). The liquid twin of
/// ScoopMath: VR cannot hit an accurate volume, so the ACTION is discretised and
/// the accuracy problem disappears — the student still prepares every tube.
///
/// The contract that makes this work: **the number in the manuscript instruction
/// IS the squeeze count, whatever its unit.** Manuscript Exp 2 measures in three
/// units — "5 drops of Ferric Chloride", "2 ml of Tollen's reagent", "0.1 g of
/// salicylic acid" — and the first two both become countable squeezes:
///     "5 drops" -> 5 squeezes   (1 squeeze = 1 drop; physically honest)
///     "2 ml"    -> 2 squeezes   (1 squeeze = 1 ml; the deliberate abstraction)
/// Bulk volumes (the 10 ml of water) stay on the tilt-pour with its tolerance
/// band; grams go to the spatula (ScoopMath). The watch panel prints the real
/// quantity for learning and the squeeze count underneath it.
///
/// Each squeeze deposits MlPerSqueeze into the target vessel through the normal
/// AddLiquid path, so an existing LiquidTaskBinding.requiredMl does the counting
/// for free ("5 drops" = requiredMl 5) — no new task plumbing, and a miscount is
/// an unambiguous graded mistake that eats finite reagent.
public static class DropperMath
{
    /// One squeeze = one unit of the instruction (one drop, or one ml).
    public const float MlPerSqueeze = 1f;

    /// A full dropper holds ten squeezes — the largest single Exp 2 instruction
    /// ("10 drops of ethanol"), so one fill covers any one step and multi-tube
    /// steps ("5 drops into each of five tubes") pace into natural refills.
    public const float Capacity = 10f;

    /// Draw only from a LIQUID store with something left, and only into an empty
    /// dropper (no topping up a partial charge — the count would be ambiguous).
    public static bool CanFill(float loadedMl, PhysicalState state, float availableMl)
        => loadedMl <= 0.001f && state == PhysicalState.Liquid && availableMl > 0.01f;

    /// A draw takes a full dropper, or whatever the bottle has left.
    public static float FillCharge(float availableMl, float capacity = Capacity)
        => Mathf.Min(Mathf.Max(0f, availableMl), capacity);

    /// Squeeze only a loaded dropper held over some OTHER container.
    public static bool CanSqueeze(float loadedMl, bool overTarget, bool sameAsSource)
        => loadedMl > 0.001f && overTarget && !sameAsSource;

    /// The last squeeze gives whatever remains rather than overdrawing.
    public static float SqueezeCharge(float loadedMl, float perSqueeze = MlPerSqueeze)
        => Mathf.Min(Mathf.Max(0f, loadedMl), perSqueeze);

    /// Squeezes a charge still has in it — what the dropper's own label shows.
    public static int SqueezesLeft(float loadedMl, float perSqueeze = MlPerSqueeze)
        => perSqueeze <= 0f ? 0 : Mathf.FloorToInt((loadedMl + 0.001f) / perSqueeze);

    /// Popup per squeeze: a running COUNT, because the count is the measurement.
    /// ("drop 3" reads as progress; a bare "+1 ml" would hide the thing being taught.)
    public static string SqueezeLabel(string chem, int dropsSoFar)
        => "drop " + dropsSoFar + "  ·  " + chem;

    /// Popup for a draw: how many squeezes the student now has in hand.
    public static string FillLabel(string chem, float chargeMl)
        => chem + "  ·  " + SqueezesLeft(chargeMl) + " drops loaded";
}

/// Dropper/pipette verb: touch a liquid reagent's bottle to draw a charge, hold
/// the dropper over a vessel and ACTIVATE (trigger) to release exactly one drop.
/// Deliberately discrete — the squeeze count IS the measurement, so it must be a
/// press, not a proximity brush like the scoop's dip.
///
/// Self-contained like ScoopController: no scene authoring beyond the component
/// (the probe works off renderer bounds; only a HELD dropper transfers, so shelf
/// contact never draws). A hand-placed "DropperTip" child wins for the probe —
/// same convention as the scoop's ScoopAnchor and the pestle's PestleTip, and
/// the same latent bug it avoids (tracking the transform ORIGIN instead of the
/// working end is what made the grind silently never complete).
public class DropperController : MonoBehaviour
{
    [SerializeField] private float probeRadius = 0.05f;
    [SerializeField] private float fillCooldown = 0.35f;
    [SerializeField] private float squeezeCooldown = 0.18f;
    [Tooltip("The dropper TIP is at the NEGATIVE end of the tool's longest axis. Flip if drops fall from the bulb end.")]
    [SerializeField] private bool tipAtPositiveEnd = false;

    private XRGrab _grab;
    private ChemicalData _loaded;
    private float _loadedMl;
    private float _readyAt;
    private int _dropCount;              // squeezes since this charge was drawn
    private LiquidPhysics _lastSource;   // the bottle just drawn from — don't instantly squeeze back into it

    public bool Loaded => _loaded != null && _loadedMl > 0.001f;
    public int SqueezesLeft => DropperMath.SqueezesLeft(_loadedMl);

    void Awake() { if (_grab == null) Bind(GetComponent<XRGrab>()); }

    /// Edit-mode / builder seam (AddComponent fires no Awake in edit mode).
    public void Bind(XRGrab grab)
    {
        _grab = grab;
        if (_grab != null)
        {
            _grab.activated.RemoveListener(OnActivated);
            _grab.activated.AddListener(OnActivated);
        }
    }

    void OnDestroy()
    {
        if (_grab != null) _grab.activated.RemoveListener(OnActivated);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        bool held = _grab != null && _grab.isSelected;
        if (!held || Loaded || Time.time < _readyAt) return;

        // Drawing up is a proximity touch (like the scoop's dip) — only the
        // RELEASE needs to be a deliberate press.
        var probe = ProbeCenter();
        foreach (var col in Physics.OverlapSphere(probe, probeRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var lp = col.GetComponentInParent<LiquidPhysics>();
            if (lp == null || lp.currentChemical == null) continue;
            if (!DropperMath.CanFill(_loadedMl, lp.currentChemical.state, lp.currentLiquidVolume)) continue;

            float charge = DropperMath.FillCharge(lp.currentLiquidVolume);
            var chem = lp.PourOut(charge);
            if (chem == null) continue;
            _loaded = chem; _loadedMl = charge; _dropCount = 0; _lastSource = lp;
            _readyAt = Time.time + fillCooldown;
            AudioService.TryPlayFirstAt(probe, 0.5f, "pour");
            FloatingText.Show(DropperMath.FillLabel(chem.chemicalName, charge),
                              probe + Vector3.up * 0.05f, new Color(0.7f, 0.9f, 1f), 0.9f);
            return;
        }
    }

    /// Trigger = ONE drop into whatever the tip is over.
    private void OnActivated(UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs _)
    {
        if (!Application.isPlaying || Time.time < _readyAt) return;
        var probe = ProbeCenter();
        LiquidPhysics target = null;
        foreach (var col in Physics.OverlapSphere(probe, probeRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var lp = col.GetComponentInParent<LiquidPhysics>();
            if (lp == null || lp == _lastSource) continue;
            target = lp; break;
        }
        if (!DropperMath.CanSqueeze(_loadedMl, target != null, false))
        {
            // Squeezing an empty dropper, or over nothing, is a no-op the player
            // should SEE — silence here reads as a broken trigger.
            if (!Loaded)
                FloatingText.Show("dropper is empty", probe + Vector3.up * 0.05f,
                                  new Color(1f, 0.8f, 0.5f), 0.7f);
            return;
        }

        float drop = DropperMath.SqueezeCharge(_loadedMl);
        target.AddLiquid(_loaded, drop);
        _loadedMl -= drop;
        _dropCount++;
        _readyAt = Time.time + squeezeCooldown;
        AudioService.TryPlayFirstAt(probe, 0.4f, "drip", "pour");
        FloatingText.Show(DropperMath.SqueezeLabel(_loaded.chemicalName, _dropCount),
                          probe + Vector3.up * 0.05f, new Color(0.6f, 1f, 0.7f), 0.7f);
        if (_loadedMl <= 0.001f) { _loaded = null; _loadedMl = 0f; _lastSource = null; }
    }

    /// The dropper TIP — a hand-placed "DropperTip" child wins, else the far end
    /// of the tool's longest axis (never the transform origin: see class note).
    private Vector3 ProbeCenter()
    {
        var anchor = transform.Find("DropperTip");
        if (anchor != null) return anchor.position;
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 axis = Matchstick.LongestLocalAxis(transform, b, out float halfLen);
        return b.center + axis * (halfLen * (tipAtPositiveEnd ? 1f : -1f));
    }
}
