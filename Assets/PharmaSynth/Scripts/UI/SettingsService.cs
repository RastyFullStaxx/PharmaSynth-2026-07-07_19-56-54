using System;
using UnityEngine;

public enum Handedness { Left, Right }

/// Plain, clamped comfort/accessibility settings (plan §3.9). Kept as a POCO so the
/// clamping + defaults are unit-testable without PlayerPrefs or a scene.
[Serializable]
public class ComfortSettings
{
    public float textScale = 1f;          // UI text multiplier
    public float subtitleSpeed = 1f;      // cutscene/subtitle pacing multiplier
    public float vignetteIntensity = 0.5f;// tunneling vignette on locomotion
    public float snapTurnAngle = 45f;     // degrees
    public bool seatedMode = false;
    public Handedness handedness = Handedness.Right;

    public void SetTextScale(float v)       => textScale = Mathf.Clamp(v, 0.8f, 1.6f);
    public void SetSubtitleSpeed(float v)   => subtitleSpeed = Mathf.Clamp(v, 0.5f, 2f);
    public void SetVignette(float v)        => vignetteIntensity = Mathf.Clamp01(v);
    public void SetSnapTurnAngle(float deg) => snapTurnAngle = Mathf.Clamp(deg, 15f, 90f);

    public ComfortSettings Clone() => (ComfortSettings)MemberwiseClone();
}

/// Owns the live ComfortSettings, persists them to PlayerPrefs, and raises Changed so
/// listeners (UI text scaler, tunneling vignette, snap-turn provider, subtitle
/// controller, locomotion) re-apply. Audio volumes live in AudioService; this is the
/// comfort/accessibility half of the Settings panel.
public class SettingsService : MonoBehaviour
{
    public static SettingsService Instance { get; private set; }

    private const string P = "settings.";
    public ComfortSettings Settings { get; private set; } = new ComfortSettings();

    /// Fired after any change (and after Load) so systems re-apply.
    public event Action<ComfortSettings> Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    public void Load()
    {
        var s = Settings;
        s.SetTextScale(PlayerPrefs.GetFloat(P + "textScale", 1f));
        s.SetSubtitleSpeed(PlayerPrefs.GetFloat(P + "subtitleSpeed", 1f));
        s.SetVignette(PlayerPrefs.GetFloat(P + "vignette", 0.5f));
        s.SetSnapTurnAngle(PlayerPrefs.GetFloat(P + "snapTurn", 45f));
        s.seatedMode = PlayerPrefs.GetInt(P + "seated", 0) == 1;
        s.handedness = PlayerPrefs.GetInt(P + "handed", 1) == 0 ? Handedness.Left : Handedness.Right;
        Raise();
    }

    public void Save()
    {
        var s = Settings;
        PlayerPrefs.SetFloat(P + "textScale", s.textScale);
        PlayerPrefs.SetFloat(P + "subtitleSpeed", s.subtitleSpeed);
        PlayerPrefs.SetFloat(P + "vignette", s.vignetteIntensity);
        PlayerPrefs.SetFloat(P + "snapTurn", s.snapTurnAngle);
        PlayerPrefs.SetInt(P + "seated", s.seatedMode ? 1 : 0);
        PlayerPrefs.SetInt(P + "handed", s.handedness == Handedness.Left ? 0 : 1);
        PlayerPrefs.Save();
    }

    // Setters used by the Settings panel controls — clamp, persist, notify.
    public void SetTextScale(float v)     { Settings.SetTextScale(v);     Commit(); }
    public void SetSubtitleSpeed(float v) { Settings.SetSubtitleSpeed(v); Commit(); }
    public void SetVignette(float v)      { Settings.SetVignette(v);      Commit(); }
    public void SetSnapTurnAngle(float d) { Settings.SetSnapTurnAngle(d); Commit(); }
    public void SetSeatedMode(bool on)    { Settings.seatedMode = on;     Commit(); }
    public void SetHandedness(Handedness h){ Settings.handedness = h;     Commit(); }
    /// Toggle wrapper: on = left-handed.
    public void SetLeftHanded(bool on)    { Settings.handedness = on ? Handedness.Left : Handedness.Right; Commit(); }

    private void Commit() { Save(); Raise(); }
    private void Raise() => Changed?.Invoke(Settings);
}
