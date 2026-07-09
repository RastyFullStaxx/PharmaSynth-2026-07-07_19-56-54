using UnityEngine;

/// Pure stride accumulator so the step cadence is unit-testable.
public static class StrideMath
{
    /// Feed horizontal distance moved; returns how many full strides completed.
    public static int Steps(ref float accumulator, float distance, float strideMeters)
    {
        if (strideMeters <= 0.01f || distance <= 0f) return 0;
        accumulator += distance;
        int n = 0;
        while (accumulator >= strideMeters) { accumulator -= strideMeters; n++; }
        return n;
    }
}

/// Plays a footstep per stride of horizontal locomotion (§4 action SFX).
/// Attach to the XR Origin; teleports/fades are ignored via a snap guard.
public class FootstepPlayer : MonoBehaviour
{
    [SerializeField] private float strideMeters = 0.75f;
    [SerializeField] private float snapGuardMeters = 2f;   // > this in one frame = teleport, not walking
    [SerializeField] private string key = "footstep";

    private Vector3 _last;
    private float _acc;
    private bool _has;

    void OnEnable() { _has = false; }

    void Update()
    {
        Vector3 p = transform.position; p.y = 0f;
        if (!_has) { _last = p; _has = true; return; }
        float d = (p - _last).magnitude;
        _last = p;
        if (d > snapGuardMeters) { _acc = 0f; return; }     // teleport/fade jump
        for (int i = StrideMath.Steps(ref _acc, d, strideMeters); i > 0; i--)
            AudioService.TryPlay(key);
    }
}
