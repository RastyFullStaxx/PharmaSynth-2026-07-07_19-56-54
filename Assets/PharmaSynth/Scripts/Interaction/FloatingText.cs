using TMPro;
using UnityEngine;

/// One-shot rising/fading world-space text (W5.8 feedback layer) — the "what
/// did my mix produce" popup, "Vessel full!", stir progress, etc. EffectVfx
/// style: static entry point, procedural, self-destroying, runtime-only.
public static class FloatingText
{
    /// Spawn a popup at `pos`. Runtime no-op in edit mode (tests use the pure
    /// format functions instead).
    public static void Show(string text, Vector3 pos, Color? color = null, float scale = 1f)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(text)) return;
        var go = new GameObject("FloatingText");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (0.02f * Mathf.Max(0.2f, scale));
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = GlyphSafe.Sanitize(text);
        tmp.fontSize = 7f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color ?? Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(6, 12, 22, 255);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 20000;   // over glass + props (world panels 4000-5000, TMP labels 20000)
        }
        go.AddComponent<FaceCamera>();
        go.AddComponent<FloatingTextFx>().Bind(tmp);
    }
}

/// Rise + fade driver for FloatingText (component so it survives the frame).
public class FloatingTextFx : MonoBehaviour
{
    private TextMeshPro _tmp;
    private float _age;
    private const float Life = 2.5f;
    private const float Rise = 0.15f;

    public void Bind(TextMeshPro tmp) => _tmp = tmp;

    private void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (Rise / Life) * Time.deltaTime;
        if (_tmp != null)
        {
            float a = 1f - Mathf.SmoothStep(0.55f, 1f, _age / Life);
            var c = _tmp.color; c.a = a; _tmp.color = c;
        }
        if (_age >= Life) Destroy(gameObject);
    }
}
