using UnityEngine;

/// Tutorial Mode's voice: the stuck-escalation ladder and the end-of-run summary.
/// Both are "what practice mode says to a struggling player", and both read the same
/// run counters, so they live together.
///
/// ⚠ Level 3 deliberately speaks the task's EXISTING hint rather than new copy: 76 of
/// 81 hint ACTION lines are voiced, and inventing coach dialogue would mean a fresh
/// voice generation pass for every mistake class.
public class TutorialCoach : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;

    public const float NudgeAfter = 15f, PulseAfter = 30f, SpeakAfter = 60f;

    private string _currentTaskId;
    private float _onStepSince;
    private int _spokenLevel;
    private int _stepsDone, _corrections;
    private bool _subscribed;

    /// Pure: 0 = silent, 1 = the watch nudge is enough, 2 = escalate the marker,
    /// 3 = say it out loud. Escalates only while the SAME step stays unsolved, so a
    /// player working steadily is never nagged.
    public static int LevelFor(float secondsOnStep)
    {
        if (secondsOnStep >= SpeakAfter) return 3;
        if (secondsOnStep >= PulseAfter) return 2;
        if (secondsOnStep >= NudgeAfter) return 1;
        return 0;
    }

    /// Pure: closure without a score. A percentage would turn practice back into an
    /// exam, which is exactly what this mode exists to avoid.
    public static string SummaryText(int stepsDone, int corrections)
    {
        string tail = corrections == 0 ? "no corrections needed."
                    : corrections == 1 ? "1 correction along the way."
                    : corrections + " corrections along the way.";
        return "Practice complete — " + stepsDone + " steps, " + tail;
    }

    /// Edit-mode seam; subscription lives here because in PLAY mode OnEnable fires
    /// before Bind() has set `runner`.
    public void Bind(ExperimentRunner r) { Detach(); runner = r; Subscribe(); }

    private void Awake() { if (runner == null) runner = FindAnyObjectByType<ExperimentRunner>(); Subscribe(); }
    private void OnEnable() => Subscribe();
    private void OnDestroy() => Detach();

    private void Subscribe()
    {
        if (_subscribed || runner == null) return;
        runner.ExperimentStarted += OnStarted;
        runner.TaskCompleted += OnTaskCompleted;
        runner.MistakeRecorded += OnMistake;
        _subscribed = true;
    }

    /// Explicit detach — DestroyImmediate skips OnDestroy for a component whose
    /// OnEnable never ran, and a ghost subscription would outlive the object.
    public void Detach()
    {
        if (!_subscribed || runner == null) { _subscribed = false; return; }
        runner.ExperimentStarted -= OnStarted;
        runner.TaskCompleted -= OnTaskCompleted;
        runner.MistakeRecorded -= OnMistake;
        _subscribed = false;
    }

    private void OnStarted(ExperimentModuleDefinition _)
    {
        _stepsDone = 0; _corrections = 0;
        _currentTaskId = null; _spokenLevel = 0; _onStepSince = Time.time;
    }

    private void OnTaskCompleted(ExperimentTask _) => _stepsDone++;
    private void OnMistake(LabErrorType _, string __) => _corrections++;

    /// Called by the gatekeeper when a practice run ends, in place of the grade screen.
    public void ShowSummary()
    {
        if (!TutorialSession.Active) return;
        Announce(SummaryText(_stepsDone, _corrections));
    }

    private void Update()
    {
        if (!TutorialSession.Active || runner == null || runner.Graph == null || !runner.IsRunning) return;
        if (TimeSkipController.IsSkipping) return;

        string id = null; string hint = null;
        foreach (var t in runner.Graph.AvailableTasks()) { id = t.taskId; hint = t.hint; break; }
        if (id != _currentTaskId)
        {
            // Progress resets the ladder — the player is moving, leave them alone.
            _currentTaskId = id; _onStepSince = Time.time; _spokenLevel = 0;
            return;
        }

        int level = LevelFor(Time.time - _onStepSince);
        if (level < 3 || _spokenLevel >= 3 || string.IsNullOrEmpty(hint)) return;
        _spokenLevel = 3;
        Announce(GlyphSafe.Sanitize(hint));
    }

    private void Announce(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        Vector3 pos = Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 1.4f
            : transform.position + Vector3.up * 1.5f;
        FloatingText.Show(msg, pos, new Color(0.7f, 1f, 0.8f), 1.3f);
        var narr = FindAnyObjectByType<NPCNarrationController>();
        if (narr != null) narr.Say(msg, 3.5f);
    }
}
