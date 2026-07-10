using UnityEngine;
using TMPro;

/// Shows a small floating name tag above an object only when the player's camera is
/// within range — so apparatus and reagents identify themselves as you approach,
/// without cluttering the lab with permanent labels. Builds its own world-space TMP
/// child on first enable; billboards to the camera.
public class ProximityLabel : MonoBehaviour
{
    [SerializeField] private string label = "";
    [SerializeField] private float showDistance = 1.4f;
    [SerializeField] private float heightOffset = 0.14f;
    [SerializeField] private float fontSize = 5.5f;

    private Transform _cam;
    private GameObject _tag;
    private TextMeshPro _tmp;
    private Renderer[] _itemRends;   // the item's own renderers, cached (no per-frame alloc)

    public void SetLabel(string text, float dist = 1.4f)
    {
        label = text; showDistance = dist;
        if (_tmp != null) _tmp.text = text;
    }

    private void Awake() => Build();

    private void Build()
    {
        if (_tag != null || string.IsNullOrEmpty(label)) return;
        // Cache the item's own renderers BEFORE the tag's TMP renderer is added,
        // so Update never re-queries the component tree (this ran every frame while
        // the label was visible — a per-frame array allocation).
        _itemRends = GetComponentsInChildren<Renderer>();
        _tag = new GameObject("ProxTag");
        _tag.transform.SetParent(transform, false);
        // Sit just above the object's top, in world scale (undo parent scaling).
        var rends = _itemRends;
        float top = 0f;
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds; for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            top = (b.max.y - transform.position.y);
        }
        _tag.transform.position = transform.position + Vector3.up * (top + heightOffset);
        var ls = transform.lossyScale;
        _tag.transform.localScale = new Vector3(1f / Mathf.Max(ls.x, 1e-4f), 1f / Mathf.Max(ls.y, 1e-4f), 1f / Mathf.Max(ls.z, 1e-4f)) * 0.02f;

        _tmp = _tag.AddComponent<TextMeshPro>();
        _tmp.text = label;
        _tmp.fontSize = fontSize;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.color = Color.white;
        _tmp.fontStyle = FontStyles.Bold;
        _tmp.outlineWidth = 0.25f;                       // dark halo → readable on any background
        _tmp.outlineColor = new Color32(6, 12, 22, 255);
        var mr = _tag.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 32760;                     // draw over glass/props
        }
        _tag.SetActive(false);
    }

    private void Update()
    {
        if (_tag == null) { Build(); if (_tag == null) return; }
        if (_cam == null)
        {
            var c = Camera.main; if (c == null) return; _cam = c.transform;
        }
        float d = Vector3.Distance(_cam.position, transform.position);
        bool show = d <= showDistance;
        if (_tag.activeSelf != show) _tag.SetActive(show);
        if (show)
        {
            // Float the tag toward the player and above the item, then billboard it.
            var rends = _itemRends;   // cached item renderers (excludes the tag's own)
            float top = transform.position.y + heightOffset;
            float mid = transform.position.y;
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                top = b.max.y + heightOffset;
                mid = b.center.y;
            }
            // Shelf-caged items: a plank right above would swallow the tag — hang it
            // at mid-height instead and pull it well out of the shelf face.
            float fwdDist = 0.08f;
            float tagY = top;
            if (Physics.Raycast(new Vector3(transform.position.x, top - heightOffset + 0.01f, transform.position.z),
                                Vector3.up, 0.35f, ~0, QueryTriggerInteraction.Ignore))
            {
                tagY = mid;
                fwdDist = 0.3f;
            }
            Vector3 toCam = (_cam.position - transform.position); toCam.y = 0f;
            Vector3 fwd = toCam.sqrMagnitude > 1e-4f ? toCam.normalized : Vector3.forward;
            _tag.transform.position = new Vector3(transform.position.x, tagY, transform.position.z) + fwd * fwdDist;
            _tag.transform.rotation = Quaternion.LookRotation(_tag.transform.position - _cam.position, Vector3.up);
        }
    }
}
