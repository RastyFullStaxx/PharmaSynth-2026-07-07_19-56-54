using UnityEngine;

/// Brings the cube spawn room to life: the neon trim breathes, the room lights drift
/// and occasionally flicker, and one strip arcs like a loose connection.
///
/// Everything is driven from ONE component with per-object phase offsets rather than
/// a pile of animators — a room where every light pulses in lockstep reads as a single
/// blinking prop, whereas offset phases read as a room that is alive. All colour work
/// goes through MaterialPropertyBlocks so no shared material is touched (the trim
/// material is used all over the room, and instancing it would leak into other scenes).
public class MenuRoomFx : MonoBehaviour
{
    [Header("Neon trim breathing")]
    [SerializeField] private Renderer[] trimRenderers;
    [SerializeField] private Color trimColour = new Color(0.30f, 0.90f, 1f);
    [SerializeField] private float trimMin = 0.55f, trimMax = 2.6f;
    [SerializeField] private float trimSpeed = 0.7f;

    [Header("Lights")]
    [SerializeField] private Light[] roomLights;
    [SerializeField] private float lightSwing = 0.25f;      // fraction of base intensity
    [SerializeField] private float lightSpeed = 0.9f;

    [Header("Arc flicker")]
    [Tooltip("Chance per second that a random trim strip stutters like a bad contact.")]
    [SerializeField] private float arcChancePerSecond = 0.35f;
    [SerializeField] private float arcSeconds = 0.14f;

    private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _mpb;
    private float[] _trimPhase;
    private float[] _lightPhase, _lightBase;
    private int _arcIndex = -1;
    private float _arcUntil;

    /// Edit-mode seam — Awake does not fire on AddComponent in edit mode.
    public void Bind(Renderer[] trim, Light[] lights)
    {
        trimRenderers = trim; roomLights = lights;
        Prepare();
    }

    private void Awake() => Prepare();

    private void Prepare()
    {
        _mpb = new MaterialPropertyBlock();

        // Phases are derived from each object's own index, NOT from Random: the room
        // must look identical every time it is entered, or the "alive" effect turns
        // into the lights having rearranged themselves while you were away.
        if (trimRenderers != null)
        {
            _trimPhase = new float[trimRenderers.Length];
            for (int i = 0; i < trimRenderers.Length; i++)
                _trimPhase[i] = i * 0.7f;
        }
        if (roomLights != null)
        {
            _lightPhase = new float[roomLights.Length];
            _lightBase = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
            {
                _lightPhase[i] = i * 1.3f;
                _lightBase[i] = roomLights[i] != null ? roomLights[i].intensity : 1f;
            }
        }
    }

    /// Pure: the breathing curve, 0..1, offset per object. Exposed so the suite can pin
    /// that it stays in range and that two different phases genuinely differ.
    public static float Breath(float time, float speed, float phase)
        => 0.5f + 0.5f * Mathf.Sin(time * speed + phase);

    private void Update()
    {
        float t = Application.isPlaying ? Time.time : 0f;

        if (trimRenderers != null)
        {
            // Pick the stuttering strip on a timer rather than every frame, so an arc
            // reads as one event instead of static.
            if (t > _arcUntil && trimRenderers.Length > 0
                && Random.value < arcChancePerSecond * Time.deltaTime)
            {
                _arcIndex = Random.Range(0, trimRenderers.Length);
                _arcUntil = t + arcSeconds;
            }

            for (int i = 0; i < trimRenderers.Length; i++)
            {
                var r = trimRenderers[i];
                if (r == null) continue;
                float k = Mathf.Lerp(trimMin, trimMax, Breath(t, trimSpeed, _trimPhase[i]));
                // The arcing strip slams between dark and overbright for a moment.
                if (i == _arcIndex && t < _arcUntil)
                    k = (Mathf.Repeat(t * 47f, 1f) < 0.5f) ? trimMax * 1.9f : trimMin * 0.2f;

                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionID, trimColour * k);
                _mpb.SetColor(BaseColorID, trimColour * Mathf.Clamp01(k));
                r.SetPropertyBlock(_mpb);
            }
        }

        if (roomLights == null) return;
        for (int i = 0; i < roomLights.Length; i++)
        {
            var l = roomLights[i];
            if (l == null) continue;
            float swing = 1f + lightSwing * (Breath(t, lightSpeed, _lightPhase[i]) * 2f - 1f);
            l.intensity = _lightBase[i] * swing;
        }
    }
}
