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

    /// The methane bench TOOLS (by name substring) that also belong to the set —
    /// they carry no methane itemId, so they were never toggled and a mortar the
    /// user parented under the stage vanished at play-start (user 2026-07-14).
    private static readonly string[] ToolNames =
    { "Motar", "Mortar", "Pestle", "Scoopula", "Spatula" };

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
        // Show through the WHOLE methane flow, not only once the run graph is live:
        // the moment methane is the chosen module (episode-pick → gear-up → armed →
        // running) the bench must be dressed (user 2026-07-14: the mortar was gone
        // when setting up). Any OTHER active experiment hides the methane set.
        bool methaneRunning = runner != null && runner.Graph != null
                              && runner.Module != null && runner.Module.moduleId == MethaneModuleId;
        bool methaneSelected = GameFlow.SelectedModuleId == MethaneModuleId;
        bool otherRunning = runner != null && runner.Module != null
                            && runner.Module.moduleId != MethaneModuleId;
        bool methaneActive = (methaneRunning || methaneSelected) && !otherRunning;
        Apply(ShouldShow(tourActive, methaneActive));
    }

    /// Find the loose methane props + bench tools once (while still active, so
    /// they're findable). Tools are also captured when they sit UNDER the stage,
    /// so we control them directly rather than relying on the parent toggle.
    private void Gather()
    {
        if (_gathered) return;
        _gathered = true;
        _looseProps.Clear();
        var seen = new HashSet<GameObject>();
        foreach (var li in FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (li == null || !PropItemIds.Contains(li.itemId)) continue;
            // Skip props already inside the stage (the stage toggle covers them);
            // capture only the ones the user moved out onto the workspace.
            if (stage != null && li.transform.IsChildOf(stage.transform)) continue;
            if (seen.Add(li.gameObject)) _looseProps.Add(li.gameObject);
        }
        // Bench tools by name (mortar/pestle/scoop) — the grind + scoop verbs live
        // on these; without them the methane bench is unusable.
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            bool isTool = false;
            foreach (var n in ToolNames) if (go.name.Contains(n)) { isTool = true; break; }
            if (!isTool) continue;
            // Only ROOT tools (a mortar's mesh child also contains "Mortar") — the one
            // with the verb component or a LabItem, not every sub-mesh.
            if (go.GetComponent<GrindController>() == null && go.GetComponent<ScoopController>() == null
                && go.GetComponent<LabItem>() == null) continue;
            if (seen.Add(go)) _looseProps.Add(go);
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
