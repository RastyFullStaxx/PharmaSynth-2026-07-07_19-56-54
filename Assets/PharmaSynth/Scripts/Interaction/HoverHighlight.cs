using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Grabbable affordance (user 2026-07-10: prop readability): when a hand/ray
/// hovers a real-scale lab tool, it brightens (base-colour tint via a
/// MaterialPropertyBlock — no per-material keyword or shader needed) and pops
/// slightly larger, so small items are easy to spot and grab; it restores on
/// hover-exit and while actually held. Thin MB over a pure scale helper.
public class HoverHighlight : MonoBehaviour
{
    [SerializeField] private float scaleFactor = 1.06f;
    [SerializeField] private Color glow = new Color(0.55f, 0.9f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float glowMix = 0.45f;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private XRGrab _grab;
    private Renderer[] _rends;
    private MaterialPropertyBlock _mpb;
    private Color[] _orig;
    private bool[] _hasBase, _hasColor;
    private Vector3 _baseScale;
    private bool _lit, _cached;

    public bool IsHighlighted => _lit;

    void Awake() { if (_grab == null) Bind(GetComponent<XRGrab>()); }

    /// Edit-mode seam (Awake doesn't fire on AddComponent in edit mode).
    public void Bind(XRGrab grab)
    {
        if (_grab != null)
        {
            _grab.hoverEntered.RemoveListener(OnHoverEnter);
            _grab.hoverExited.RemoveListener(OnHoverExit);
            _grab.selectEntered.RemoveListener(OnSelect);
        }
        _grab = grab;
        if (_grab != null)
        {
            _grab.hoverEntered.AddListener(OnHoverEnter);
            _grab.hoverExited.AddListener(OnHoverExit);
            _grab.selectEntered.AddListener(OnSelect);
        }
        Cache();
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.hoverEntered.RemoveListener(OnHoverEnter);
            _grab.hoverExited.RemoveListener(OnHoverExit);
            _grab.selectEntered.RemoveListener(OnSelect);
        }
    }

    private void Cache()
    {
        if (_cached) return;
        _rends = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;
        _orig = new Color[_rends.Length];
        _hasBase = new bool[_rends.Length];
        _hasColor = new bool[_rends.Length];
        for (int i = 0; i < _rends.Length; i++)
        {
            var mat = _rends[i] != null ? _rends[i].sharedMaterial : null;
            _hasBase[i] = mat != null && mat.HasProperty(BaseColorID);
            _hasColor[i] = mat != null && mat.HasProperty(ColorID);
            _orig[i] = _hasBase[i] ? mat.GetColor(BaseColorID)
                     : (_hasColor[i] ? mat.GetColor(ColorID) : Color.white);
        }
        _cached = true;
    }

    // Global throttle so sweeping a ray across a shelf of items doesn't machine-gun
    // the hover blip — at most one hover tick per this many seconds, lab-wide.
    private const float HoverSfxInterval = 0.09f;
    private static float _lastHoverSfx = -1f;

    private void OnHoverEnter(HoverEnterEventArgs _)
    {
        SetHighlight(true);
        if (Application.isPlaying && Time.unscaledTime - _lastHoverSfx >= HoverSfxInterval)
        {
            _lastHoverSfx = Time.unscaledTime;
            AudioService.TryPlay("hover");
        }
    }
    private void OnHoverExit(HoverExitEventArgs _) => SetHighlight(false);
    private void OnSelect(SelectEnterEventArgs _)
    {
        SetHighlight(false);                       // grabbed → drop the glow
        if (Application.isPlaying) AudioService.TryPlay("grab");   // universal grab/hold cue
    }

    /// Pure scale rule (self-tested): grow by factor while lit, back to base otherwise.
    public static Vector3 HighlightScale(Vector3 baseScale, bool on, float factor)
        => on ? baseScale * Mathf.Max(1f, factor) : baseScale;

    /// Toggle the hover look. Public so tests / other affordance drivers can call it.
    public void SetHighlight(bool on)
    {
        Cache();
        if (on == _lit) return;
        _lit = on;
        transform.localScale = HighlightScale(_baseScale, on, scaleFactor);
        if (_rends == null) return;
        for (int i = 0; i < _rends.Length; i++)
        {
            if (_rends[i] == null) continue;
            _rends[i].GetPropertyBlock(_mpb);
            Color c = on ? Color.Lerp(_orig[i], glow, glowMix) : _orig[i];
            if (_hasBase[i]) _mpb.SetColor(BaseColorID, c);
            if (_hasColor[i]) _mpb.SetColor(ColorID, c);
            _rends[i].SetPropertyBlock(_mpb);
        }
    }
}
