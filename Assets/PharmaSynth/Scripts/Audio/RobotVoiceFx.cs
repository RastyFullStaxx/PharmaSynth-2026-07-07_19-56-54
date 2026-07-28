using UnityEngine;

/// Pharmee is a FEMALE ROBOT, and text-to-speech gives you a natural human read
/// (user 2026-07-27: "it sounds really like a lady and not Pharmee"). No voice id
/// fixes that — TTS models are trained toward human naturalness — so the robot
/// character is applied as a FILTER on his voice channel instead of being baked
/// into the files. That keeps the generated masters clean and re-tunable: change
/// these numbers and every line in the game changes with them, including lines
/// not generated yet. Nothing here costs a credit.
///
/// Ring modulation is the ingredient that actually reads as "robot" — multiplying
/// the voice by a low sine adds the metallic sideband buzz. Unity has no built-in
/// ring modulator, hence this filter; the softer colouring (chorus, band limits)
/// comes from Unity's stock AudioSource filters, wired by the builder menu.
///
/// Deliberately kept INTELLIGIBLE: this is an educational game where the guide
/// explains procedure, so the effect is blended with dry signal rather than run
/// at full depth. Raise ringMix past ~0.6 and it starts costing comprehension.
[RequireComponent(typeof(AudioSource))]
public class RobotVoiceFx : MonoBehaviour
{
    [Header("Shared tuning")]
    [Tooltip("The ONE profile every Pharmee channel reads. Tune it once and every line " +
             "he speaks follows — including lines not generated yet. Leave empty to fall " +
             "back to the local values below (used only if the asset is missing).")]
    public RobotVoiceProfile profile;

    [Header("Fallback (used only when no profile is assigned)")]
    [Range(20f, 200f)] public float ringHz = 62f;
    [Range(0f, 1f)] public float ringMix = 0.38f;
    [Range(0f, 2f)] public float presenceBoost = 0.85f;
    [Range(500f, 8000f)] public float presenceHz = 3000f;
    [Range(4f, 16f)] public float crushBits = 10f;
    [Range(0f, 1f)] public float crushMix = 0.35f;
    public bool active = true;

    /// Live values: the shared profile wins, so one asset drives every channel.
    public float RingHz => profile != null ? profile.ringHz : ringHz;
    public float RingMix => profile != null ? profile.ringMix : ringMix;
    public float PresenceBoost => profile != null ? profile.presenceBoost : presenceBoost;
    public float PresenceHz => profile != null ? profile.presenceHz : presenceHz;
    public float CrushBits => profile != null ? profile.crushBits : crushBits;
    public float CrushMix => profile != null ? profile.crushMix : crushMix;
    public bool Active => profile != null ? profile.active : active;

    private double _phase;
    private int _sampleRate = 48000;
    // Per-channel low-band memory for the presence shelf (8 covers any layout).
    private readonly float[] _lowBand = new float[8];

    private void Awake() { _sampleRate = AudioSettings.outputSampleRate; }
    private void OnEnable() { _phase = 0d; }

    /// Pure (suite-pinned): one ring-modulated sample. `carrier` is the sine value
    /// at this instant; the dry signal is blended back in so speech stays readable.
    public static float RingSample(float dry, float carrier, float mix)
    {
        mix = Mathf.Clamp01(mix);
        return dry * (1f - mix) + dry * carrier * mix;
    }

    /// Pure (suite-pinned): quantise to `bits` and blend. Bit-crushing is the right
    /// grit for a robot — it adds high-frequency aliasing, so unlike distortion it
    /// makes the voice MORE crisp rather than duller (the muffling complaint,
    /// user 2026-07-27). 16 bits is transparent; 8-10 has audible character.
    public static float Crush(float x, float bits, float mix)
    {
        mix = Mathf.Clamp01(mix);
        if (mix <= 0f) return x;
        float levels = Mathf.Pow(2f, Mathf.Clamp(bits, 2f, 16f));
        float step = 2f / levels;
        float q = Mathf.Round(x / step) * step;
        return x * (1f - mix) + q * mix;
    }

    /// Pure (suite-pinned): smoothing coefficient for a one-pole filter at `hz`.
    public static float OnePoleAlpha(float hz, int sampleRate)
    {
        if (sampleRate <= 0) return 0.5f;
        float a = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Max(1f, hz) / sampleRate);
        return Mathf.Clamp01(a);
    }

    /// Audio-thread callback: runs on every buffer of whatever this AudioSource
    /// plays. Allocation-free by necessity — do not add logging or LINQ here.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Read the shared values ONCE per buffer — this is the audio thread.
        float mix = RingMix, boost = PresenceBoost, cMix = CrushMix, cBits = CrushBits;
        if (!Active) return;
        if (mix <= 0.0001f && boost <= 0.0001f && cMix <= 0.0001f) return;
        if (_sampleRate <= 0) _sampleRate = 48000;

        float alpha = OnePoleAlpha(PresenceHz, _sampleRate);
        double step = 2d * System.Math.PI * RingHz / _sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            float carrier = (float)System.Math.Sin(_phase);
            _phase += step;
            if (_phase > 2d * System.Math.PI) _phase -= 2d * System.Math.PI;

            // Same carrier across the frame's channels, so stereo stays coherent.
            for (int c = 0; c < channels; c++)
            {
                float x = data[i + c];

                // 1. PRESENCE first, while the signal is still clean: split off the
                //    band above presenceHz with a one-pole and add it back. Doing
                //    this before the grit is what keeps consonants legible.
                int ci = c & 7;
                _lowBand[ci] += alpha * (x - _lowBand[ci]);
                x += (x - _lowBand[ci]) * boost;

                // 2. Digital grit — machine character that BRIGHTENS.
                x = Crush(x, cBits, cMix);

                // 3. Ring modulation — the actual robot.
                x = RingSample(x, carrier, mix);

                // The lift and the crush can both push past full scale; clamp so a
                // loud line never clips into a crackle.
                data[i + c] = x < -1f ? -1f : (x > 1f ? 1f : x);
            }
        }
    }
}
