using UnityEngine;

/// Concrete face for Pharmee: tints the screen-face renderer(s) per expression.
/// Implements IPharmeeFace so PharmeeBrain/PharmeeGatekeeper drive it. Point
/// faceRenderers at the robot's eye/mouth meshes; the color property matches the
/// shader (_EmissionColor for an emissive screen, else _BaseColor). Uses a
/// MaterialPropertyBlock — no material instantiation, edit-mode safe.
/// Default = HAPPY (user 2026-07-10: happy by default, especially while following).
public class PharmeeFace : MonoBehaviour, IPharmeeFace
{
    [SerializeField] private Renderer faceRenderer;      // legacy single (kept wired)
    [SerializeField] private Renderer[] faceRenderers;   // eyes + mouth meshes
    [SerializeField] private string colorProperty = "_EmissionColor";
    [SerializeField] private PharmeeFaceExpression defaultExpression = PharmeeFaceExpression.Happy;
    [ColorUsage(true, true)] [SerializeField] private Color neutral = new Color(0.2f, 0.9f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color happy = new Color(0.3f, 1f, 0.5f);
    [ColorUsage(true, true)] [SerializeField] private Color warning = new Color(1f, 0.6f, 0.15f);

    private MaterialPropertyBlock _mpb;

    public PharmeeFaceExpression Current { get; private set; } = PharmeeFaceExpression.Neutral;

    /// Editor-builder seam: point the face at the screen meshes.
    public void BindRenderers(params Renderer[] rs) { faceRenderers = rs; }

    private void Start() => ResetToDefault();

    /// Back to the resting mood (happy) — PharmeeMood calls this when a line ends.
    public void ResetToDefault() => SetExpression(defaultExpression);

    public void SetExpression(PharmeeFaceExpression e)
    {
        Current = e;
        Color c = e == PharmeeFaceExpression.Happy ? happy
                : e == PharmeeFaceExpression.Warning ? warning
                : neutral;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Apply(faceRenderer, c);
        if (faceRenderers != null)
            foreach (var r in faceRenderers) Apply(r, c);
    }

    private void Apply(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(colorProperty, c);
        if (colorProperty == "_EmissionColor") _mpb.SetColor("_BaseColor", c * 0.4f);
        r.SetPropertyBlock(_mpb);
    }
}
