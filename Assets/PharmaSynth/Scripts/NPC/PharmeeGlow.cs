using UnityEngine;

/// Pure rules for Pharmee's body glow (suite-pinned).
public static class PharmeeGlowMath
{
    /// Readable, not blazing. The bloom threshold is 1.0, so a peak just above it gives a
    /// faint halo — a lit robot — where the model's authored 8.0 gave a white ball.
    public const float DefaultIntensity = 1.45f;

    /// The emission colour to write: the hue scaled to an HDR intensity, alpha pinned.
    public static Color Emission(Color hue, float intensity)
    {
        var c = hue * Mathf.Max(0f, intensity);
        c.a = 1f;
        return c;
    }

    /// Which renderers this component owns: the body's light panels and the four hover
    /// rings. The eyes and mouth share the same material but belong to PharmeeFace, which
    /// colours them per expression — two writers on one renderer would fight.
    public static bool IsGlowPart(string rendererName)
        => rendererName == "Robot_Blue_Light_0"
           || (rendererName.StartsWith("Wave") && rendererName.EndsWith("_Blue_Light_0"));
}

/// Dims the light Pharmee gives off (user 2026-09-05: "too flashy when she speaks").
///
/// The robot's FBX ships one embedded material, `Blue_Light`, with an HDR emission of
/// (0.46, 2.75, 8.0) — eight times white — on the body's light panels and all four hover
/// rings. Only the eyes and mouth were ever overridden (PharmeeFace); the rest bloomed into
/// a white ball that wobbled with every talk nod. This writes a readable emission through a
/// MaterialPropertyBlock, the same pattern PharmeeFace uses: no material is instanced, the
/// FBX and its importer stay untouched, and `intensity` is tunable live in the inspector.
/// Thin over PharmeeGlowMath; Bind() seam because AddComponent fires no Awake in edit mode.
public class PharmeeGlow : MonoBehaviour
{
    [SerializeField] private Renderer[] glowRenderers = new Renderer[0];
    [Tooltip("The panel/ring hue. Emission = hue x intensity.")]
    [SerializeField] private Color hue = new Color(0.06f, 0.35f, 1f);
    [Tooltip("HDR intensity. 1.0 = no bloom at all; ~1.45 = a faint halo; 8 = the authored blaze.")]
    [SerializeField] private float intensity = PharmeeGlowMath.DefaultIntensity;

    private MaterialPropertyBlock _mpb;

    public Renderer[] Renderers => glowRenderers;
    public float Intensity => intensity;
    public Color Hue => hue;

    /// Editor-builder seam.
    public void BindRenderers(params Renderer[] rs) { glowRenderers = rs ?? new Renderer[0]; Apply(); }

    private void Start() => Apply();
    private void OnValidate() => Apply();

    public void Apply()
    {
        if (glowRenderers == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Color e = PharmeeGlowMath.Emission(hue, intensity);
        foreach (var r in glowRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", e);
            r.SetPropertyBlock(_mpb);
        }
    }
}
