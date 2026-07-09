using UnityEngine;

/// Driver for the HUD canvas. Two modes:
///  ScreenLocked (default, storyboard style) — the canvas is rigidly glued to the
///    camera every frame (full rotation), so pills sit at fixed screen corners.
///  LazyFollow — comfort mode: hovers ahead and only re-centres outside a deadzone
///    (kept as an option; some players find head-locked UI fatiguing on-device).
public class HudRigController : MonoBehaviour
{
    public enum Mode { ScreenLocked, LazyFollow }

    [SerializeField] private Mode mode = Mode.ScreenLocked;
    [SerializeField] private Camera cameraOverride;              // falls back to Camera.main
    [SerializeField] private float lockedDistance = 1.1f;        // metres in front of the lens
    [SerializeField] private HudFollowSolver.Params follow = HudFollowSolver.Params.Default;

    private HudFollowSolver.State _state;
    private bool _snapped;

    public Mode CurrentMode { get => mode; set => mode = value; }
    public HudFollowSolver.Params Follow { get => follow; set => follow = value; }

    public void SetCamera(Camera c) => cameraOverride = c;

    private Camera Cam => cameraOverride != null ? cameraOverride : Camera.main;

    /// Place the HUD directly on its anchor (scene start, after teleports).
    public void SnapToCamera()
    {
        var cam = Cam;
        if (cam == null) return;
        if (mode == Mode.ScreenLocked)
        {
            ApplyLocked(cam);
        }
        else
        {
            var t = cam.transform;
            _state = HudFollowSolver.Snapped(t.position, t.eulerAngles.y, in follow);
            ApplyFollow();
        }
        _snapped = true;
    }

    private void LateUpdate()
    {
        var cam = Cam;
        if (cam == null) return;
        if (mode == Mode.ScreenLocked)
        {
            ApplyLocked(cam);
            return;
        }
        if (!_snapped) { SnapToCamera(); return; }
        var head = cam.transform;
        HudFollowSolver.Step(ref _state, head.position, head.eulerAngles.y, in follow, Time.deltaTime);
        ApplyFollow();
    }

    private void ApplyLocked(Camera cam)
    {
        var camT = cam.transform;
        transform.SetPositionAndRotation(
            camT.position + camT.rotation * new Vector3(0f, 0f, lockedDistance),
            camT.rotation);
        // Fit the canvas to the camera frustum at this distance, so corner-anchored
        // children hug the TRUE viewport edges at any aspect ratio / FOV.
        var rt = transform as RectTransform;
        if (rt == null) return;
        float h = 2f * lockedDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * Mathf.Max(cam.aspect, 0.1f);
        float s = Mathf.Max(transform.localScale.x, 1e-5f);
        rt.sizeDelta = new Vector2(w / s, h / s);
    }

    private void ApplyFollow()
    {
        transform.SetPositionAndRotation(_state.pos, Quaternion.Euler(0f, _state.yawDeg, 0f));
    }
}
