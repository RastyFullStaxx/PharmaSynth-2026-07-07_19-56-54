using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Pure rules for scooping solids (W5.12, user: "some reagents are needed to be
/// scooped… scoop adds a specific amount per scoop… the visual increases and so
/// does the scale text"). Kept plain so the suite pins the policy.
public static class ScoopMath
{
    /// Fixed charge one dip transfers, in grams (1 g/ml proxy — WeighMath's
    /// convention, so the balance display and VesselStatus read consistently).
    public const float GramsPerScoop = 2f;

    /// The manuscript's PORCELAIN SPATULA charge. Exp 2 weighs 0.1 g of salicylic
    /// acid and 0.5 g of aspirin — both SMALLER than one 2 g scoopful, so the
    /// scoop simply cannot express them. A spatula dip is 0.1 g, making those
    /// 1 and 5 dips: the same "the number in the instruction is the action count"
    /// contract the dropper uses for drops (user 2026-07-16).
    public const float GramsPerSpatula = 0.1f;

    /// A dip picks up ONLY from a solid/powder store with something left, and
    /// only while the scoop is empty (no double-dipping a full scoop).
    public static bool CanPickUp(bool carrying, PhysicalState state, float availableMl)
        => !carrying && (state == PhysicalState.Solid || state == PhysicalState.Powder)
           && availableMl > 0.01f;

    /// The last scoopful takes whatever remains.
    public static float ScoopCharge(float availableMl, float perScoop = GramsPerScoop)
        => Mathf.Min(Mathf.Max(0f, availableMl), perScoop);

    /// A carried charge deposits into any OTHER container (empty or not — the
    /// vessel's own capacity/reaction rules take over via AddLiquid).
    public static bool CanDeposit(bool carrying, bool sameContainer)
        => carrying && !sameContainer;

    /// Popup for a deposit: running total so repeated scoops read as progress.
    public static string DepositLabel(string chem, float addedG, float totalG)
        => "+" + addedG.ToString("0.#") + " g " + chem + "  (" + totalG.ToString("0.#") + " g total)";
}

/// Scoopula/spatula verb: dip into a solid reagent's jar to pick up a fixed
/// charge (a visible tinted heap rides the blade), touch a receiving vessel to
/// deposit it — per-scoop FloatingText totals, and the balance/VesselStatus
/// update through the normal contents path. Self-contained: no scene authoring
/// needed beyond adding the component (the proximity probe works off renderer
/// bounds; only a HELD scoop transfers, so shelf contact never scoops).
public class ScoopController : MonoBehaviour
{
    [SerializeField] private float probeRadius = 0.055f;
    [SerializeField] private float actionCooldown = 0.5f;
    [Tooltip("Grams per dip. ScoopMath.GramsPerScoop (2 g) for the scoopula; ScoopMath.GramsPerSpatula (0.1 g) for the porcelain spatula, which is the only tool that can express Exp 2's 0.1 g / 0.5 g weighings.")]
    [SerializeField] private float gramsPerDip = ScoopMath.GramsPerScoop;
    [Tooltip("The scooping BLADE/BOWL is at the NEGATIVE end of the tool's longest axis (user 2026-07-14: the heap was riding the handle butt). Flip if the heap rides the handle.")]
    [SerializeField] private bool bladeAtPositiveEnd = false;
    [Tooltip("Shape of the carried heap on the blade/bowl (local scale). The SCOOPULA gets a rounded bowl-pile (default); the porcelain SPATULA a flat elongated smear (set by Apply W5.8 Verb Data). The two tools shouldn't look the same (user 2026-07-18).")]
    [SerializeField] private Vector3 heapScale = new Vector3(0.026f, 0.018f, 0.026f);   // rounded bowl-pile (scoopula)

    private XRGrab _grab;
    private ChemicalData _carrying;
    private float _carryingG;
    private float _readyAt;
    private GameObject _heap;
    private LiquidPhysics _lastSource;   // the jar we just dipped — don't instantly re-deposit into it

    public bool Carrying => _carrying != null;

    void Awake() { if (_grab == null) Bind(GetComponent<XRGrab>()); }

    /// Edit-mode / builder seam.
    public void Bind(XRGrab grab) => _grab = grab;

    /// Builder seam for the finer porcelain-spatula charge (ScoopMath.GramsPerSpatula).
    public void SetGramsPerDip(float grams) { if (grams > 0f) gramsPerDip = grams; }

    /// Read seam (suite): which charge this tool actually dips.
    public float GramsPerDip => gramsPerDip;

    /// Builder seam: the porcelain spatula's flat elongated smear vs the scoopula's
    /// rounded bowl-pile. Read seam pins the two tools differ.
    public void SetHeapScale(Vector3 s) { if (s.sqrMagnitude > 1e-8f) heapScale = s; }
    public Vector3 HeapScale => heapScale;

    void Update()
    {
        if (!Application.isPlaying) return;
        bool held = _grab != null && _grab.isSelected;
        if (!held || Time.time < _readyAt) return;

        // Forgiving probe: anything near the blade counts as touched.
        var probe = ProbeCenter();
        var cols = Physics.OverlapSphere(probe, probeRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var col in cols)
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var lp = col.GetComponentInParent<LiquidPhysics>();
            if (lp == null) continue;
            if (!Carrying)
            {
                if (lp.currentChemical == null
                    || !ScoopMath.CanPickUp(false, lp.currentChemical.state, lp.currentLiquidVolume)) continue;
                float charge = ScoopMath.ScoopCharge(lp.currentLiquidVolume, gramsPerDip);
                var chem = lp.PourOut(charge);
                if (chem == null) continue;
                _carrying = chem; _carryingG = charge; _lastSource = lp;
                _readyAt = Time.time + actionCooldown;
                ShowHeap(chem);
                RefreshPowder(lp);                                  // source mound shrinks
                // GRANULAR, never liquid (user 2026-07-15): a dry blade-into-powder
                // dig. No "pour" fallback — silence beats the wrong material's sound.
                AudioService.TryPlayFirstAt(probe, 0.55f, "scoop");
                FloatingText.Show("+" + charge.ToString("0.#") + " g " + chem.chemicalName,
                                  probe + Vector3.up * 0.05f, new Color(1f, 0.95f, 0.6f), 0.8f);
                return;
            }
            if (!ScoopMath.CanDeposit(true, lp == _lastSource)) continue;
            var deposited = _carrying;
            lp.AddLiquid(_carrying, _carryingG);
            RefreshPowder(lp);                                      // receiver mound grows
            // Solid tipping out of the scoop — a granular patter, not a liquid pour.
            AudioService.TryPlayFirstAt(probe, 0.85f, "powder-pour", "scoop");
            FloatingText.Show(ScoopMath.DepositLabel(deposited.chemicalName, _carryingG, lp.currentLiquidVolume),
                              probe + Vector3.up * 0.05f, new Color(0.6f, 1f, 0.7f), 0.8f);
            _carrying = null; _carryingG = 0f; _lastSource = null;
            _readyAt = Time.time + actionCooldown;
            HideHeap();
            return;
        }
        // Once the loaded scoop has LEFT its source jar, forget it — so a later
        // deliberate return to the same jar deposits back instead of being
        // mistaken for the original dip.
        if (Carrying && _lastSource != null)
        {
            bool stillTouching = false;
            foreach (var col in cols)
                if (col != null && col.GetComponentInParent<LiquidPhysics>() == _lastSource) stillTouching = true;
            if (!stillTouching) _lastSource = null;
        }
    }

    /// The scooping BLADE tip — the far end of the tool's longest axis (user
    /// 2026-07-13: the pick-up probe and the carried heap were at the whole-tool
    /// centre, so the powder appeared to ride the middle of the handle and the
    /// bowl never seemed to touch the jar). Now both live on the blade.
    private Vector3 ProbeCenter()
    {
        // Hand-placed "ScoopAnchor" child wins (drag it onto the bowl/blade; user
        // 2026-07-14) — otherwise use the far end of the tool's longest axis.
        var anchor = transform.Find("ScoopAnchor");
        if (anchor != null) return anchor.position;
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 axis = Matchstick.LongestLocalAxis(transform, b, out float halfLen);
        return b.center + axis * (halfLen * (bladeAtPositiveEnd ? 1f : -1f));
    }

    /// Small tinted mound riding the blade while a charge is carried.
    private void ShowHeap(ChemicalData chem)
    {
        if (_heap == null)
        {
            _heap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _heap.name = "ScoopHeap";
            var hc = _heap.GetComponent<Collider>();
            if (hc != null) Destroy(hc);
            _heap.transform.SetParent(transform, false);
            _heap.transform.localScale = heapScale;   // per-tool: rounded bowl-pile vs flat smear
            var r = _heap.GetComponent<Renderer>();
            r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ScoopHeap_Runtime" };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        _heap.transform.position = ProbeCenter() + Vector3.up * 0.008f;
        var rend = _heap.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            var c = chem != null ? chem.liquidColor : Color.gray; c.a = 1f;
            if (rend.sharedMaterial.HasProperty("_BaseColor")) rend.sharedMaterial.SetColor("_BaseColor", c);
            else rend.sharedMaterial.color = c;
        }
        _heap.SetActive(true);
    }

    private void HideHeap() { if (_heap != null) _heap.SetActive(false); }

    /// Keep a solid/powder container's mound in sync with its contents so it grows
    /// as scoops go in and shrinks as they come out (user 2026-07-14: "content
    /// should appear nicely and increase as more is poured in").
    private void RefreshPowder(LiquidPhysics lp)
    {
        if (lp == null) return;
        var c = lp.currentChemical;
        if (c == null || (c.state != PhysicalState.Solid && c.state != PhysicalState.Powder)) return;
        float fill = Mathf.Clamp01(lp.currentLiquidVolume / 20f);
        if (lp.currentLiquidVolume > 0.01f) fill = Mathf.Max(0.28f, fill);   // one scoop is still visible
        ExperimentSceneBuilder.EnsurePowderVisual(lp.gameObject, c, fill);
    }
}
