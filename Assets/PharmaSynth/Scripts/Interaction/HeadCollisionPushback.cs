using UnityEngine;

/// Stops the player's HEAD from phasing through static geometry, no matter how the
/// camera moved — thumbstick locomotion, the XR Device Simulator's direct HMD
/// translate (which bypasses the CharacterController by design), or physically
/// leaning through a wall. Each frame the head's path is swept as a small sphere;
/// if it would cross static geometry, the whole rig is pulled back so the head
/// stays on the outside. Triggers, the rig's own colliders, and dynamic props
/// (anything with a Rigidbody) never push back.
public class HeadCollisionPushback : MonoBehaviour
{
    [SerializeField] private Transform head;          // the XR camera (falls back to Camera.main)
    [SerializeField] private Transform rig;           // XR Origin root (falls back to this transform)
    [SerializeField] private float headRadius = 0.14f;
    [Tooltip("A jump larger than this is a teleport/scene move — accepted, not blocked.")]
    [SerializeField] private float teleportThreshold = 1.0f;

    private Vector3 _lastValid;
    private bool _has;

    private static readonly RaycastHit[] Hits = new RaycastHit[16];

    public void Bind(Transform headT, Transform rigT) { head = headT; rig = rigT; _has = false; }

    private void LateUpdate()
    {
        var rigT = rig != null ? rig : transform;
        var headT = head;
        if (headT == null)
        {
            var c = Camera.main;
            if (c == null) return;
            headT = head = c.transform;
        }

        Vector3 target = headT.position;
        if (!_has) { _lastValid = target; _has = true; return; }

        Vector3 delta = target - _lastValid;
        float dist = delta.magnitude;
        if (dist < 1e-5f) return;
        if (dist > teleportThreshold)               // scripted teleport / respawn — accept
        {
            _lastValid = target;
            return;
        }

        Vector3 dir = delta / dist;
        int n = Physics.SphereCastNonAlloc(_lastValid, headRadius, dir, Hits, dist, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var h = Hits[i];
            if (h.collider == null) continue;
            if (h.rigidbody != null) continue;                          // dynamic props never wall the player
            if (h.collider.transform.IsChildOf(rigT)) continue;         // the rig's own body
            if (h.distance < nearest) nearest = h.distance;
        }

        if (nearest < float.MaxValue)
        {
            // Head tried to cross static geometry: allow travel up to the surface,
            // then shift the RIG back by the overshoot so the head stays outside.
            Vector3 allowed = _lastValid + dir * Mathf.Max(0f, nearest - 0.02f);
            Vector3 correction = allowed - target;
            rigT.position += correction;
            _lastValid = allowed;
        }
        else
        {
            _lastValid = target;
        }
    }
}
