using UnityEngine;

/// ONE shared tuning for Pharmee's robot voice (user 2026-07-27: "make it one
/// reusable effect so once we tune it, it applies to other voices of Pharmee").
///
/// Every RobotVoiceFx in the scene reads this asset, so there is a single place
/// to dial the character and it reaches every line Pharmee will ever speak —
/// the ones already generated and the ones not generated yet. Tuning never costs
/// a credit and never requires regenerating audio.
///
/// It is a ScriptableObject on purpose: values changed while PLAY is running are
/// written to the asset and SURVIVE exiting play mode, unlike edits to a scene
/// component, which are discarded. So you can tune it by ear in a live session
/// and keep what you land on.
[CreateAssetMenu(menuName = "PharmaSynth/Robot Voice Profile", fileName = "RobotVoiceProfile")]
public class RobotVoiceProfile : ScriptableObject
{
    [Tooltip("Ring-mod carrier in Hz. 40-60 = heavy machine; 70-110 = lighter synthetic chirp.")]
    [Range(20f, 200f)] public float ringHz = 62f;

    [Tooltip("Blend of ring-modulated signal. 0 = untouched human read, 1 = full Dalek. " +
             "0.3-0.45 reads as robot while staying intelligible — this is an educational " +
             "game and the guide explains procedure, so comprehension outranks character.")]
    [Range(0f, 1f)] public float ringMix = 0.38f;

    [Header("Clarity (2026-07-27 — he was coming out muffled)")]
    [Tooltip("Presence lift: how much of the HIGH band above presenceHz is added back. " +
             "This is what keeps consonants crisp — a robot should sound like a machine, " +
             "not like someone talking through a blanket. 0 = off, 0.6-1.2 = bright.")]
    [Range(0f, 2f)] public float presenceBoost = 0.85f;

    [Tooltip("Where the presence lift starts, in Hz. 2500-3500 is the consonant band.")]
    [Range(500f, 8000f)] public float presenceHz = 3000f;

    [Tooltip("Bit-crush depth. Quantisation grit reads as DIGITAL and, unlike distortion, " +
             "it adds high-frequency content instead of dulling — so it makes him more " +
             "machine AND more crisp at the same time. 16 = clean, 8-10 = characterful.")]
    [Range(4f, 16f)] public float crushBits = 10f;

    [Tooltip("How much crushed signal is blended in. 0 = off.")]
    [Range(0f, 1f)] public float crushMix = 0.35f;

    [Tooltip("Off = hear the raw text-to-speech read, for A/B comparison.")]
    public bool active = true;
}
