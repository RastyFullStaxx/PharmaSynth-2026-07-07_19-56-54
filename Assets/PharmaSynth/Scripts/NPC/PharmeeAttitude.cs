using UnityEngine;

/// Pharmee flight attitude (user 2026-07-10): lean the body into the movement
/// direction so he reads as flying through air, pulse the hover waves at his
/// base, and add a gentle bob-nod while he talks. Composes with FaceCamera
/// (root yaw) and FloatBob (position) by twisting only the body CHILD.
///
/// W5.38: also folds in the GESTURE pose (PharmeeGestureMath) and aims the model's two hand
/// pivots. Everything still lands in the ONE localRotation assignment below - this class is
/// the sole writer of bodyRoot, and a second component writing it would silently lose or
/// fight per-frame. The model has no skeleton (no skins, no joints, no LimbNodes in either
/// RobotNPC.glb or .fbx), so all of this is procedural by necessity as well as by choice.
public class PharmeeAttitude : MonoBehaviour
{
    [SerializeField] private Transform bodyRoot;                 // "Robot Origin"
    [SerializeField] private Transform[] waves = new Transform[0];
    [SerializeField] private FloatBob bob;                       // velocity source (home glide)
    [SerializeField] private NPCNarrationController narration;   // talk-time motion
    [SerializeField] private float maxLeanDeg = 14f;
    [SerializeField] private float leanPerMps = 22f;             // degrees per m/s
    [SerializeField] private float leanSharpness = 5f;
    [SerializeField] private float wavePulse = 0.12f;
    [SerializeField] private float waveSpeedMultiplier = 30f;    // user 2026-07-10: waves 30x faster

    [Header("Talk motion (W5.38)")]
    // These were HARDCODED at 2.5 and 1.8 degrees against a 14-degree lean cap, i.e. not
    // actually visible. Serialized now because the readable amplitude is a headset call.
    [SerializeField] private float talkPitchDegrees = 6.5f;
    [SerializeField] private float talkRollDegrees = 3.5f;

    [Header("Gestures (W5.38)")]
    [SerializeField] private Transform handLeft;                 // model node "Hand origin"
    [SerializeField] private Transform handRight;                // model node "Hand origin.002"
    [Tooltip("Degrees the hand pivots swing up at full raise.")]
    [SerializeField] private float handRaiseDegrees = 55f;
    [SerializeField] private float handSharpness = 8f;

    private Quaternion _baseLocal;
    private Vector3[] _waveBase = new Vector3[0];
    private Vector3 _prevHome;
    private bool _hasPrev, _talking, _cached;
    private float _lean;
    private Vector3 _leanAxis = Vector3.forward;
    private PharmeePose _pose = PharmeePose.Rest;
    private Quaternion _handLeftBase = Quaternion.identity, _handRightBase = Quaternion.identity;
    private float _handRaise;
    private bool _handsCached;

    public void Bind(Transform body, Transform[] waveRings, FloatBob b, NPCNarrationController n)
    { bodyRoot = body; waves = waveRings ?? new Transform[0]; bob = b; narration = n; _cached = false; }

    /// The model's two hand pivots. Nothing had ever moved them before W5.38.
    public void BindHands(Transform left, Transform right)
    { handLeft = left; handRight = right; _handsCached = false; }

    /// Fed each frame by PharmeeGestures. Held (not consumed) so a dropped frame just
    /// repeats the last pose rather than snapping to rest.
    public void SetPose(PharmeePose pose) => _pose = pose;

    /// Pure lean curve — self-tests pin it.
    public static float LeanFor(float speedMps, float degPerMps, float maxDeg)
        => Mathf.Min(Mathf.Max(0f, speedMps) * degPerMps, maxDeg);

    void OnEnable()
    {
        if (narration != null)
        {
            narration.LineStarted += OnLineStarted;
            narration.LineEnded += OnLineEnded;
        }
    }

    void OnDisable()
    {
        if (narration != null)
        {
            narration.LineStarted -= OnLineStarted;
            narration.LineEnded -= OnLineEnded;
        }
    }

    void OnLineStarted(string line, float seconds) => _talking = true;
    void OnLineEnded() => _talking = false;

    void Cache()
    {
        if (_cached || bodyRoot == null) return;
        _baseLocal = bodyRoot.localRotation;
        _waveBase = new Vector3[waves.Length];
        for (int i = 0; i < waves.Length; i++)
            if (waves[i] != null) _waveBase[i] = waves[i].localScale;
        _cached = true;
    }

    void CacheHands()
    {
        if (_handsCached) return;
        if (handLeft != null) _handLeftBase = handLeft.localRotation;
        if (handRight != null) _handRightBase = handRight.localRotation;
        _handsCached = handLeft != null || handRight != null;
    }

    void LateUpdate()
    {
        Cache();
        if (bodyRoot == null) return;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // Velocity from the glide home (bob/jitter noise excluded).
        Vector3 vel = Vector3.zero;
        if (bob != null)
        {
            Vector3 home = bob.Home;
            if (_hasPrev) vel = (home - _prevHome) / dt;
            _prevHome = home; _hasPrev = true;
        }
        vel.y = 0f;

        float targetLean = LeanFor(vel.magnitude, leanPerMps, maxLeanDeg);
        if (vel.sqrMagnitude > 1e-6f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, vel.normalized);   // tips the top INTO the motion
            _leanAxis = bodyRoot.parent != null ? bodyRoot.parent.InverseTransformDirection(axis) : axis;
        }
        _lean = Mathf.Lerp(_lean, targetLean, leanSharpness * dt);

        Quaternion talk = Quaternion.identity;
        if (_talking)
        {
            float t = Time.time;
            talk = Quaternion.Euler(Mathf.Sin(t * 3.1f) * talkPitchDegrees,
                                    0f,
                                    Mathf.Sin(t * 2.3f) * talkRollDegrees);
        }
        // ONE assignment, four composed terms. Adding a term here is the whole reason a
        // separate gesture component would have been wrong.
        bodyRoot.localRotation = Quaternion.AngleAxis(_lean, _leanAxis) * talk * _pose.bodyRot * _baseLocal;

        // Hands: eased so a gesture starting mid-frame does not snap them.
        CacheHands();
        _handRaise = Mathf.Lerp(_handRaise, _pose.handRaise, Mathf.Clamp01(handSharpness * dt));
        if (handLeft != null || handRight != null)
        {
            var swing = Quaternion.Euler(-handRaiseDegrees * _handRaise, 0f, 0f);
            if (handLeft != null) handLeft.localRotation = _handLeftBase * swing;
            if (handRight != null) handRight.localRotation = _handRightBase * swing;
        }

        // Hover waves: staggered breathing pulse, faster while moving.
        float rate = (2.2f + Mathf.Min(vel.magnitude * 2f, 2f)) * Mathf.Max(0.01f, waveSpeedMultiplier);
        for (int i = 0; i < waves.Length && i < _waveBase.Length; i++)
        {
            if (waves[i] == null) continue;
            float s = 1f + wavePulse * Mathf.Sin(Time.time * rate + i * 0.9f);
            waves[i].localScale = _waveBase[i] * s * _pose.waveFlare;   // celebrate flares the rings
        }
    }
}
