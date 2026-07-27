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
    public bool active = true;

    /// Live values: the shared profile wins, so one asset drives every channel.
    public float RingHz => profile != null ? profile.ringHz : ringHz;
    public float RingMix => profile != null ? profile.ringMix : ringMix;
    public bool Active => profile != null ? profile.active : active;

    private double _phase;
    private int _sampleRate = 48000;

    private void Awake() { _sampleRate = AudioSettings.outputSampleRate; }
    private void OnEnable() { _phase = 0d; }

    /// Pure (suite-pinned): one ring-modulated sample. `carrier` is the sine value
    /// at this instant; the dry signal is blended back in so speech stays readable.
    public static float RingSample(float dry, float carrier, float mix)
    {
        mix = Mathf.Clamp01(mix);
        return dry * (1f - mix) + dry * carrier * mix;
    }

    /// Audio-thread callback: runs on every buffer of whatever this AudioSource
    /// plays. Allocation-free by necessity — do not add logging or LINQ here.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Read the shared values ONCE per buffer — this is the audio thread.
        float mix = RingMix;
        if (!Active || mix <= 0.0001f) return;
        if (_sampleRate <= 0) _sampleRate = 48000;

        double step = 2d * System.Math.PI * RingHz / _sampleRate;
        for (int i = 0; i < data.Length; i += channels)
        {
            float carrier = (float)System.Math.Sin(_phase);
            _phase += step;
            if (_phase > 2d * System.Math.PI) _phase -= 2d * System.Math.PI;
            // Same carrier across the frame's channels, so stereo stays coherent.
            for (int c = 0; c < channels; c++)
                data[i + c] = RingSample(data[i + c], carrier, mix);
        }
    }
}
