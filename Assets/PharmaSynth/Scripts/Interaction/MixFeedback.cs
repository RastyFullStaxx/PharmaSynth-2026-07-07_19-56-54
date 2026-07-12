using UnityEngine;

/// "What did my mix produce?" feedback on a vessel (W5.8): a registered
/// reaction pops its authored observation text + a colour flash; a harmless
/// no-rule mix reports the mixture story; an overflow says "Vessel full!" and
/// wets the bench. Hazardous mixes stay SILENT here — HazardousMixReactor owns
/// that consequence theatre (smoke/fire/alarm) and double-messaging would
/// bury it. Feedback only: no new graded mistakes.
public class MixFeedback : MonoBehaviour
{
    private LiquidPhysics _lp;
    private float _quietUntil;   // throttle chatty events (per-vessel)

    /// Pure policy the suite pins: only non-hazardous no-rule mixes announce.
    public static bool ShouldAnnounceWrongMix(HazardousMix.HazardOutcome outcome)
        => outcome == HazardousMix.HazardOutcome.None;

    /// Builder seam.
    public void Bind(LiquidPhysics lp)
    {
        if (_lp != null)
        {
            _lp.ReactionOccurred -= OnReaction;
            _lp.WrongReagentMixed -= OnWrongMix;
            _lp.LiquidRejected -= OnRejected;
        }
        _lp = lp;
        if (_lp == null) return;
        _lp.ReactionOccurred += OnReaction;
        _lp.WrongReagentMixed += OnWrongMix;
        _lp.LiquidRejected += OnRejected;
    }

    private void OnDestroy() { if (_lp != null) Bind(null); }

    private Vector3 PopupPos()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return transform.position + Vector3.up * 0.2f;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return new Vector3(b.center.x, b.max.y + 0.08f, b.center.z);
    }

    private void OnReaction(ReactionRule rule)
    {
        if (rule == null) return;
        string text = !string.IsNullOrEmpty(rule.expectedObservation)
            ? rule.expectedObservation
            : "Reaction: " + (rule.resultLiquid != null ? rule.resultLiquid.chemicalName : "product formed");
        Color c = rule.resultLiquid != null ? rule.resultLiquid.liquidColor : new Color(0.6f, 0.9f, 1f);
        FloatingText.Show(text, PopupPos(), Color.white);
        if (Application.isPlaying) EffectVfx.ColorFlash(PopupPos(), c);
    }

    private void OnWrongMix(ChemicalData current, ChemicalData incoming)
    {
        if (!ShouldAnnounceWrongMix(HazardousMix.Classify(current, incoming))) return;   // reactor owns hazards
        if (Time.time < _quietUntil) return;
        _quietUntil = Time.time + 2.5f;
        string story = _lp != null ? _lp.Ledger.Summary(3) : "";
        FloatingText.Show(string.IsNullOrEmpty(story) ? "Mixed — no reaction" : "Mixed: " + story + " — no reaction",
                          PopupPos(), new Color(1f, 0.85f, 0.5f));
    }

    private void OnRejected(ChemicalData incoming, float ml)
    {
        if (Time.time < _quietUntil) return;
        _quietUntil = Time.time + 2.5f;
        FloatingText.Show("Vessel full!", PopupPos(), new Color(1f, 0.6f, 0.5f));
        if (Application.isPlaying && _lp != null)
        {
            Color c = incoming != null ? incoming.liquidColor : new Color(0.55f, 0.7f, 0.85f);
            SpillPuddle.Spawn(transform.position + Vector3.up * 0.01f, c, 0.08f);
        }
    }
}
