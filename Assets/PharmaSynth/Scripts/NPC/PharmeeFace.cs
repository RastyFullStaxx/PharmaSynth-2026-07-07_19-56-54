using UnityEngine;

/// Concrete face for Pharmee: tints the screen-face renderer per expression.
/// Implements IPharmeeFace so PharmeeBrain drives it. Point faceRenderer at the
/// robot's screen mesh and set the color property to match its shader
/// (_EmissionColor for an emissive screen, else _BaseColor).
public class PharmeeFace : MonoBehaviour, IPharmeeFace
{
    [SerializeField] private Renderer faceRenderer;
    [SerializeField] private string colorProperty = "_EmissionColor";
    [ColorUsage(true, true)] [SerializeField] private Color neutral = new Color(0.2f, 0.9f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color happy = new Color(0.3f, 1f, 0.5f);
    [ColorUsage(true, true)] [SerializeField] private Color warning = new Color(1f, 0.6f, 0.15f);

    public void SetExpression(PharmeeFaceExpression e)
    {
        if (faceRenderer == null) return;
        Color c = e == PharmeeFaceExpression.Happy ? happy
                : e == PharmeeFaceExpression.Warning ? warning
                : neutral;
        var mat = faceRenderer.material; // runtime instance
        if (mat.HasProperty(colorProperty))
        {
            mat.SetColor(colorProperty, c);
            if (colorProperty == "_EmissionColor") mat.EnableKeyword("_EMISSION");
        }
    }
}
