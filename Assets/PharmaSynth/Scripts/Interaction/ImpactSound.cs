using UnityEngine;

/// Material-aware drop clatter (§4 action SFX): a FREE (dynamic) item landing
/// plays its material's clip — glass clinks, metal clatters, wood knocks.
/// Impacts at glass-breaking speed stay silent here; BreakableGlassware plays
/// the shatter instead. (No RequireComponent: colliders often live on children;
/// collision events still route to the Rigidbody host.)
public class ImpactSound : MonoBehaviour
{
    [SerializeField] private string key = "drop-wood";
    [SerializeField] private float minSpeed = 0.7f;
    [SerializeField] private float maxSpeed = float.PositiveInfinity;
    [SerializeField] private float cooldownSeconds = 0.3f;

    private Rigidbody _rb;
    private float _readyAt;

    void Awake() { if (_rb == null) _rb = GetComponent<Rigidbody>(); }

    public void Bind(Rigidbody rb, string soundKey, float breakSpeedCeiling = float.PositiveInfinity)
    { _rb = rb; key = soundKey; maxSpeed = breakSpeedCeiling; }

    void OnCollisionEnter(Collision c)
    {
        if (_rb == null || _rb.isKinematic) return;
        if (Time.time < _readyAt) return;
        float v = c.relativeVelocity.magnitude;
        if (v < minSpeed || v >= maxSpeed) return;
        _readyAt = Time.time + cooldownSeconds;
        AudioService.TryPlay(key);
    }
}
