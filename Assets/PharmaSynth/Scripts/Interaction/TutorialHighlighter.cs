using System.Collections.Generic;
using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Drives Tutorial Mode's guidance off the task graph: whatever step is available
/// right now, its objects glow.
///
/// ⭐ It NEVER decides that a step is DONE. This is a pure read of AvailableTasks();
/// the task completes through its existing binding exactly as in campaign, and the
/// glow follows on the next poll because the available set changed. Since no parallel
/// completion detector exists, the guidance cannot disagree with the game — which is
/// the whole failure mode the W5.34 hint audit was spent unpicking.
public class TutorialHighlighter : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private float pollSeconds = 0.2f;   // 5 Hz — per-frame is pure waste
    [SerializeField] private float regrabDelay = 0.5f;   // dropped-unused flicker guard

    private readonly Dictionary<Transform, TaskTarget> _lit = new Dictionary<Transform, TaskTarget>();
    private readonly Dictionary<Transform, float> _droppedAt = new Dictionary<Transform, float>();
    private readonly Dictionary<Transform, TaskTarget> _wanted = new Dictionary<Transform, TaskTarget>();
    private float _nextPoll;

    private bool _subscribed;

    /// Edit-mode seam — Awake/OnEnable do not fire on AddComponent in edit mode.
    /// The subscription lives HERE and not in OnEnable alone: in PLAY mode
    /// AddComponent fires OnEnable immediately, before Bind() has set `runner`.
    public void Bind(ExperimentRunner r) { Detach(); runner = r; Subscribe(); }

    private void Awake() { if (runner == null) runner = FindAnyObjectByType<ExperimentRunner>(); Subscribe(); }
    private void OnEnable() => Subscribe();

    private void Subscribe()
    {
        if (_subscribed || runner == null) return;
        runner.ExperimentStarted += OnRunBegan;
        runner.ExperimentPrepared += OnRunBegan;      // armed seam: stage is already up
        _subscribed = true;
    }

    /// Explicit detach: DestroyImmediate skips OnDisable/OnDestroy for a component
    /// whose OnEnable never ran, and a ghost subscription would outlive the object.
    public void Detach()
    {
        if (!_subscribed || runner == null) { _subscribed = false; return; }
        runner.ExperimentStarted -= OnRunBegan;
        runner.ExperimentPrepared -= OnRunBegan;
        _subscribed = false;
    }

    private void OnDestroy() => Detach();

    /// The stage is furnished during the launcher's onModuleLoaded, which fires before
    /// the runner starts — so by here every task-bearing component exists to be swept.
    private void OnRunBegan(ExperimentModuleDefinition _)
    {
        if (!TutorialSession.Active) return;
        TutorialTargets.Build();
        _lit.Clear();
        _droppedAt.Clear();
    }

    /// Pure rule: should this target be glowing right now?
    /// A SOURCE goes quiet the moment it is in hand — you got the message, and leaving
    /// it lit while you carry it just adds noise. A DESTINATION or TOOL stays lit,
    /// because holding the right test tube is not the same as finishing the step.
    public static bool ShouldLight(TaskTarget target, bool held, bool taskAvailable)
    {
        if (!taskAvailable) return false;
        return !held || target.stayLitWhenHeld;
    }

    /// Pure gate: a longProcess step fades the screen to black, and a glowing bottle
    /// floating on black reads as a bug rather than a hint.
    public static bool GuidanceAllowed(bool running, bool skipping) => running && !skipping;

    private void Update()
    {
        if (!TutorialSession.Active) { ClearAll(); return; }
        if (Time.time < _nextPoll) return;
        _nextPoll = Time.time + pollSeconds;

        bool running = runner != null && runner.Graph != null && runner.IsRunning;
        if (!GuidanceAllowed(running, TimeSkipController.IsSkipping)) { ClearAll(); return; }

        // Self-heal: a restart path that re-furnishes the stage without re-firing
        // ExperimentStarted would otherwise leave us pointing at destroyed objects.
        if (TaskTargetRegistry.TaskCount == 0) TutorialTargets.Build();

        _wanted.Clear();
        foreach (var task in runner.Graph.AvailableTasks())
        {
            // EVERY available task, not just the first: the graph can open parallel
            // branches, and hiding one would teach the player a stricter order than
            // the experiment actually has. (The waypoint still shows a single arrow.)
            var targets = TaskTargetRegistry.Targets(task.taskId);
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.transform == null) continue;
                bool held = IsHeld(t.transform);
                if (held) { _droppedAt.Remove(t.transform); }
                else if (!_lit.ContainsKey(t.transform)
                         && _droppedAt.TryGetValue(t.transform, out float when)
                         && Time.time - when < regrabDelay)
                {
                    continue;      // just fumbled it — hold off re-lighting for a beat
                }

                if (ShouldLight(t, held, true)) _wanted[t.transform] = t;
                else if (held) _droppedAt[t.transform] = Time.time;
            }
        }

        foreach (var kv in _lit)                                   // leaving the set
            if (kv.Key != null && !_wanted.ContainsKey(kv.Key)) SetGuide(kv.Key, false, kv.Value.role);
        foreach (var kv in _wanted)                                // entering the set
            if (!_lit.ContainsKey(kv.Key)) SetGuide(kv.Key, true, kv.Value.role);

        _lit.Clear();
        foreach (var kv in _wanted) _lit[kv.Key] = kv.Value;
    }

    /// Held right now? XRI's own select state is the truth — DropRespawn and the rack
    /// snapping both freeze released objects kinematic, so a rigidbody check would lie.
    public static bool IsHeld(Transform t)
    {
        if (t == null) return false;
        var grab = t.GetComponent<XRGrab>();
        return grab != null && grab.isSelected;
    }

    private static void SetGuide(Transform t, bool on, TargetRole role)
    {
        if (t == null) return;
        var hh = t.GetComponent<HoverHighlight>();
        if (hh != null) hh.SetGuide(on, role);
    }

    private void ClearAll()
    {
        if (_lit.Count == 0) return;
        foreach (var kv in _lit) SetGuide(kv.Key, false, kv.Value.role);
        _lit.Clear();
        _droppedAt.Clear();
    }

    private void OnDisable() => ClearAll();
}
