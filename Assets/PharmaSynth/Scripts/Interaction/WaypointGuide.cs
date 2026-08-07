using UnityEngine;

/// Floats a marker/beacon above the station for the current available step,
/// guiding the player where to go next (storyboard: "follow the markers").
/// Hides when nothing is available (between steps / finished).
public class WaypointGuide : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private Transform marker;
    [SerializeField] private float heightOffset = 0.55f;

    public void SetRunner(ExperimentRunner r) => runner = r;

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
            marker.position = station.position + Vector3.up * heightOffset;
        }
        else Hide();
    }

    private void Hide()
    {
        if (marker != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
    }
}
