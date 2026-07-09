using UnityEngine;

/// Real-time dressing mirror (user 2026-07-10): a quad on the wall showing the
/// live scene from the player's REFLECTED eye through an off-axis (portal)
/// projection, horizontally flipped — i.e. a physically-correct planar mirror.
/// The render camera's near plane sits ON the mirror, so wall geometry behind
/// it never occludes. Distance-gated and half-res for the Quest budget.
public class MirrorPlane : MonoBehaviour
{
    [SerializeField] private Camera mirrorCam;       // child camera (no listener)
    [SerializeField] private Renderer surface;       // the mirror quad
    [SerializeField] private int rtWidth = 512, rtHeight = 768;
    [SerializeField] private float activeDistance = 5.5f;
    [SerializeField] private LayerMask cullMask = ~(1 << 5);   // everything but UI (HUD)

    private RenderTexture _rt;
    private Material _mat;
    private Transform _head;

    void Start()
    {
        if (mirrorCam == null || surface == null) return;
        _rt = new RenderTexture(rtWidth, rtHeight, 16);
        _rt.name = "MirrorRT";
        mirrorCam.targetTexture = _rt;
        mirrorCam.cullingMask = cullMask;
        mirrorCam.stereoTargetEye = StereoTargetEyeMask.None;
        _mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _mat.mainTexture = _rt;
        _mat.mainTextureScale = new Vector2(-1f, 1f);   // mirror parity flip
        _mat.mainTextureOffset = new Vector2(1f, 0f);
        surface.material = _mat;
    }

    void OnDestroy() { if (_rt != null) _rt.Release(); }

    void LateUpdate()
    {
        if (mirrorCam == null || surface == null) return;
        if (_head == null)
        {
            var c = Camera.main;
            if (c == null) return;
            _head = c.transform;
        }

        var t = surface.transform;
        Vector3 center = t.position;
        Vector3 nRoom = -t.forward;                       // quad's visible face (-Z) → into the room
        Vector3 toHead = _head.position - center;

        // Gate: far away, or standing behind the wall → skip the render.
        bool active = toHead.magnitude < activeDistance && Vector3.Dot(toHead, nRoom) > 0.05f;
        if (mirrorCam.enabled != active) mirrorCam.enabled = active;
        if (!active) return;

        // Virtual eye: the head reflected through the mirror plane.
        float dist = Vector3.Dot(toHead, nRoom);
        Vector3 eye = _head.position - 2f * dist * nRoom;

        // Off-axis (portal) frustum whose window is exactly the quad.
        float w = t.lossyScale.x, h = t.lossyScale.y;
        Vector3 up = t.up;
        Vector3 right = Vector3.Cross(up, nRoom).normalized;   // viewer-right for someone facing nRoom
        Vector3 pa = center - right * (w * 0.5f) - up * (h * 0.5f);   // bottom-left as seen from the eye
        Vector3 va = pa - eye;
        Vector3 vb = pa + right * w - eye;
        Vector3 vc = pa + up * h - eye;
        float d = Vector3.Dot(va, nRoom);                 // eye is behind the plane; nRoom points at it? No:
        d = Mathf.Abs(Vector3.Dot(va, nRoom));            // distance from virtual eye to the plane
        if (d < 0.02f) return;
        float near = d, far = 25f;
        float l = Vector3.Dot(right, va) * near / d;
        float r = Vector3.Dot(right, vb) * near / d;
        float b = Vector3.Dot(up, va) * near / d;
        float tt = Vector3.Dot(up, vc) * near / d;

        mirrorCam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(nRoom, up));
        var p = Matrix4x4.Frustum(l, r, b, tt, near, far);
        mirrorCam.projectionMatrix = p;
    }
}
