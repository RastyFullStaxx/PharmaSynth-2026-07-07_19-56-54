using System.Collections.Generic;
using UnityEngine;

/// W5.12 (user 2026-07-13): the hand-built Methane stage should be PRESENT only
/// during the Lab Tour and the Methane tutorial itself, and gone the moment you
/// move on to any other experiment or are idle in the lab. Methane is the only
/// experiment that uses this stage (Experiment 1), so a single controller owns
/// its visibility — the sole authority (ExperimentSceneBuilder no longer toggles
/// it). Lives on a manager object, NOT on the stage (it must keep running while
/// the stage is hidden). Edit mode leaves the stage as-authored so it can be
/// relocated by hand; this only governs Play.
///
/// The user hand-moved the 5 methane PROPS out of the stage hierarchy onto the
/// workspace, so toggling the stage alone would leave them visible ("leaking").
/// This also gathers those loose props by their methane itemIds and toggles them
/// alongside the stage — no reparenting, so the hand placement is untouched.
public class MethaneStageVisibility : MonoBehaviour
{
    [SerializeField] private GameObject stage;
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private LabTourGuide tour;

    public const string MethaneModuleId = "tutorial-methane";

    /// The methane-only props (by LabItem.itemId) that must hide with the stage.
    private static readonly HashSet<string> PropItemIds = new HashSet<string>
    { "burner", "glass-tube", "collection-tube", "reagent-jar", "lit-splint" };

    private readonly List<GameObject> _looseProps = new List<GameObject>();
    private bool _gathered;

    /// Pure rule (suite-pinned): visible during the Lab Tour OR while the Methane
    /// attempt is the active one; hidden otherwise.
    public static bool ShouldShow(bool tourActive, bool methaneAttemptActive)
        => tourActive || methaneAttemptActive;

    /// Builder/test seam.
    public void Bind(GameObject stage, ExperimentRunner runner, LabTourGuide tour)
    { this.stage = stage; this.runner = runner; this.tour = tour; }

    private void Start()
    {
        Gather();
        Apply(false);   // hidden by default at lab entry
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        bool tourActive = tour != null && tour.IsActive;
        bool methaneActive = runner != null && runner.Graph != null
                             && runner.Module != null && runner.Module.moduleId == MethaneModuleId;
        Apply(ShouldShow(tourActive, methaneActive));
    }

    /// Find the loose methane props once (while still active, so they're findable).
    private void Gather()
    {
        if (_gathered) return;
        _gathered = true;
        _looseProps.Clear();
        foreach (var li in FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (li == null || !PropItemIds.Contains(li.itemId)) continue;
            // Skip props already inside the stage (the stage toggle covers them);
            // capture only the ones the user moved out onto the workspace.
            if (stage != null && li.transform.IsChildOf(stage.transform)) continue;
            _looseProps.Add(li.gameObject);
        }
    }

    private void Apply(bool show)
    {
        if (stage != null && stage.activeSelf != show) stage.SetActive(show);
        for (int i = 0; i < _looseProps.Count; i++)
        {
            var p = _looseProps[i];
            if (p != null && p.activeSelf != show) p.SetActive(show);
        }
    }
}
