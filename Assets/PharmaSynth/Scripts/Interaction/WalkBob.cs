using UnityEngine;

/// Subtle head-bob while the player is locomoting, for a grounded, "walking" feel.
/// Applied to the XR camera's local offset so it never fights head tracking — and
/// kept small + speed-scaled for VR comfort (bob only when actually moving).
/// Amplitude is intentionally low; expose it so a comfort setting can zero it out.
public class WalkBob : MonoBehaviour
{
    [Tooltip("The XR Origin root whose planar movement drives the bob.")]
    [SerializeField] private Transform originRoot;
    [Tooltip("Transform to bob — usually the Camera Offset or the camera itself.")]
    [SerializeField] private Transform bobTarget;

    [SerializeField] private float amplitude = 0.018f;   // metres — deliberately subtle
    [SerializeField] private float stride = 9f;          // bob cycles per metre of travel
    [SerializeField, Range(0f, 1f)] private float rollDegrees = 0.6f;
    [SerializeField] private float moveThreshold = 0.05f; // m/s below which we idle
    [SerializeField] private float settle = 6f;           // how fast the bob eases in/out

    private Vector3 _lastPos;
    private Vector3 _homeLocal;
    private Quaternion _homeRot;
    private float _phase;
    private float _weight;

    public void SetAmplitude(float a) => amplitude = a;   // comfort toggle → 0

    private void Start()
    {
        if (originRoot == null) originRoot = transform;
        if (bobTarget == null) bobTarget = transform;
        _homeLocal = bobTarget.localPosition;
        _homeRot = bobTarget.localRotation;
        _lastPos = originRoot.position;
    }

    private void LateUpdate()
    {
        if (bobTarget == null || originRoot == null) return;

        Vector3 now = originRoot.position;
        Vector3 delta = now - _lastPos; delta.y = 0f;
        _lastPos = now;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);

        bool moving = speed > moveThreshold;
        _weight = Mathf.MoveTowards(_weight, moving ? 1f : 0f, Time.deltaTime * settle);
        if (moving) _phase += delta.magnitude * stride;

        float bob = Mathf.Sin(_phase) * amplitude * _weight;
        float roll = Mathf.Cos(_phase * 0.5f) * rollDegrees * _weight;

        bobTarget.localPosition = _homeLocal + new Vector3(0f, bob, 0f);
        // roll relative to the cached home rotation (never compounds frame-to-frame)
        bobTarget.localRotation = _homeRot * Quaternion.Euler(0f, 0f, roll);
    }
}
