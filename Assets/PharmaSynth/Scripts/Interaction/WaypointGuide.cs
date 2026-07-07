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
        if (marker == null || runner == null || runner.Graph == null || !runner.IsRunning)
        {
            Hide();
            return;
        }

        string id = null;
        foreach (var t in runner.Graph.AvailableTasks()) { id = t.taskId; break; }
        CurrentTargetTaskId = id;

        Transform station = ExperimentStationRegistry.Get(id);
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
