using UnityEngine;

/// Looping apparatus audio for a sim-driven station (§4): boil bubbles while
/// heating, drips while filtering, hiss while collecting gas, shimmer while
/// crystallising. Owns a positional AudioSource; ZoneSimStation drives
/// SetRunning from zone occupancy. Safe with missing clips (silent no-op).
public class SimLoopAudio : MonoBehaviour
{
    [SerializeField] private string key = "";

    private AudioSource _src;
    private bool _prepared;

    /// SoundBank key per sim verb — pure so the self-tests pin the mapping.
    public static string KeyFor(StationSim kind)
    {
        switch (kind)
        {
            case StationSim.Heat: return "bubble";
            case StationSim.Crystallise: return "crystallise";
            case StationSim.Filter: return "filter-drip";
            case StationSim.Collect: return "gas-hiss";
            default: return "";
        }
    }

    public void Bind(string soundKey) => key = soundKey;

    public bool IsPlaying => _src != null && _src.isPlaying;

    public void SetRunning(bool on)
    {
        if (on && !_prepared) Prepare();
        if (_src == null) return;
        if (on && !_src.isPlaying) _src.Play();
        else if (!on && _src.isPlaying) _src.Stop();
    }

    void Prepare()
    {
        _prepared = true;
        var svc = AudioService.Instance;
        var entry = svc != null ? svc.EntryOf(key) : null;
        if (entry == null || entry.clip == null) return;
        _src = gameObject.AddComponent<AudioSource>();
        _src.clip = entry.clip;
        _src.loop = true;
        _src.playOnAwake = false;
        _src.spatialBlend = 1f;                 // positional: heard at the station
        _src.maxDistance = 6f;
        _src.rolloffMode = AudioRolloffMode.Linear;
        _src.volume = entry.volume * (svc != null ? svc.VolumeOf(AudioCategory.Sfx) : 1f);
    }

    void OnDisable() { if (_src != null && _src.isPlaying) _src.Stop(); }
}
