using UnityEngine;

/// Floats a marker/beacon above the station for the current available step,
/// guiding the player where to go next (storyboard: "follow the markers").
/// Hides when nothing is available (between steps / finished).
public class WaypointGuide : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private Transform marker;

    [Header("Placement")]
    [Tooltip("Clearance above the target's SOLID TOP — not its transform origin, which for a shelf bottle sits at its base.")]
    [SerializeField] private float heightOffset = 0.16f;
    [Tooltip("If anything is within this distance straight above, the target is shelf-caged and the marker moves in FRONT instead.")]
    [SerializeField] private float shelfClearance = 0.4f;
    [Tooltip("How far to pull the marker out toward the player when caged.")]
    [SerializeField] private float frontDistance = 0.3f;
    [Tooltip("Marker size multiplier — 1 = whatever the beacon prefab was authored at.")]
    [SerializeField] private float markerScale = 3.2f;

    /// The marker's ORIGINAL authored scale, captured exactly once and SERIALIZED.
    /// It has to persist: the multiplier is applied to it every frame, so re-deriving
    /// the "home" size from the marker's current scale would multiply an already
    /// multiplied value — the builder is re-run often, and each run would compound.
    /// Zero means "not captured yet".
    [SerializeField, HideInInspector] private Vector3 markerHomeScale = Vector3.zero;

    private Transform _cam;

    public void SetRunner(ExperimentRunner r) => runner = r;

    /// Set by Build Tutorial Scene Wiring so the tuned size lives in ONE place. The
    /// field is [SerializeField], so a value already saved in the scene would otherwise
    /// win over any new code default and the change would look like it did nothing.
    public void SetMarkerScale(float s)
    {
        markerScale = s;
        ApplyMarkerScale();
    }

    /// Push the tuned placement into the scene. Necessary, not merely tidy: these are
    /// [SerializeField]s that ALREADY existed in the saved scene, so Unity keeps the
    /// old serialized numbers and a changed C# default silently does nothing —
    /// heightOffset sat at the original 0.55 long after the default became 0.16.
    public void SetPlacement(float height, float clearance, float front)
    {
        heightOffset = height;
        shelfClearance = clearance;
        frontDistance = front;
    }

    private void ApplyMarkerScale()
    {
        if (marker == null) return;
        if (markerHomeScale == Vector3.zero) markerHomeScale = marker.localScale;
        marker.localScale = markerHomeScale * Mathf.Max(0.01f, markerScale);
    }

    /// Pure placement rule (suite-pinned).
    ///
    /// Anchored to the target's SOLID BOUNDS, never its transform origin: a shelf
    /// bottle's origin sits at its base, so "origin + up" buried the marker in the
    /// bottle itself (user headset capture, 2026-08-07).
    ///
    /// When something is directly overhead — the shelf plank above, in a cabinet —
    /// going up hides the marker inside it. Caged targets get the marker pulled OUT
    /// toward the player at mid height instead, the same escape ProximityLabel makes
    /// for exactly the same reason.
    public static Vector3 MarkerPosition(Bounds solid, float heightOffset, bool caged,
                                         Vector3 camPos, float frontDistance)
    {
        if (!caged)
            return new Vector3(solid.center.x, solid.max.y + heightOffset, solid.center.z);

        Vector3 toCam = camPos - solid.center; toCam.y = 0f;
        Vector3 fwd = toCam.sqrMagnitude > 1e-4f ? toCam.normalized : Vector3.forward;
        return solid.center + fwd * frontDistance;
    }

    public string CurrentTargetTaskId { get; private set; }

    private void Update()
    {
        // Guidance is a Tutorial Mode affordance. Campaign shows no beacon — finding
        // the apparatus is part of what it assesses.
        if (marker == null || runner == null || runner.Graph == null || !runner.IsRunning
            || !TutorialSession.Active || TimeSkipController.IsSkipping)
        {
            Hide();
            return;
        }

        string id = null;
        foreach (var t in runner.Graph.AvailableTasks()) { id = t.taskId; break; }
        CurrentTargetTaskId = id;

        // ONE arrow, never two — pick the SOURCE first ("go fetch that"), and hop to
        // the destination once it is in hand ("now put it here"). Two simultaneous
        // arrows would just ask the player which one to follow.
        Transform station = null;
        var targets = TaskTargetRegistry.Targets(id);
        for (int i = 0; i < targets.Count && station == null; i++)
            if (targets[i].role == TargetRole.Source && targets[i].transform != null
                && !TutorialHighlighter.IsHeld(targets[i].transform))
                station = targets[i].transform;
        for (int i = 0; i < targets.Count && station == null; i++)
            if (targets[i].transform != null) station = targets[i].transform;

        if (station != null)
        {
            if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            ApplyMarkerScale();

            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;

            // SolidWorldBounds, not all renderers: LiquidPourer's StreamLine/PourStream
            // are world-space and outlive a pour pointing at the floor, which would drag
            // the top of the bounds down a metre and drop the marker through the bench.
            Bounds solid = ExperimentSceneBuilder.SolidWorldBounds(station.gameObject);
            bool caged = Physics.Raycast(
                new Vector3(solid.center.x, solid.max.y + 0.01f, solid.center.z),
                Vector3.up, shelfClearance, ~0, QueryTriggerInteraction.Ignore);

            marker.position = MarkerPosition(solid, heightOffset, caged,
                _cam != null ? _cam.position : marker.position, frontDistance);
        }
        else Hide();
    }

    private void Hide()
    {
        if (marker != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
    }
}
