using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// Plays data-driven cutscenes at the right narrative moments by subscribing to
/// the ExperimentRunner: Intro when the experiment starts, ReagentPrep when that
/// phase completes, and Success/Failure when it finishes. Beats are delivered as
/// Pharmee subtitles + face expressions (VR-safe; no camera animation).
///
/// The end-cutscene ALWAYS plays (success OR failure variant) — a user requirement.
public class CutsceneDirector : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private NPCNarrationController narration;
    [SerializeField] private MonoBehaviour faceBehaviour; // optional IPharmeeFace

    [Header("Cutscenes")]
    [Tooltip("Optional: moduleId→cutscene-set lookup. When set, the director swaps its four cutscenes to match each experiment as it starts. Falls back to the fields below when a module has no entry.")]
    [SerializeField] private CutsceneLibrary library;
    [SerializeField] private CutsceneData intro;
    [SerializeField] private CutsceneData reagentPrep;
    [SerializeField] private CutsceneData success;
    [SerializeField] private CutsceneData failure;

    // Test/inspection accessors.
    public CutsceneData Intro => intro;
    public CutsceneData ReagentPrep => reagentPrep;
    public CutsceneData Success => success;
    public CutsceneData Failure => failure;
    public void SetLibrary(CutsceneLibrary lib) => library = lib;

    public UnityEvent onCutsceneStarted;
    public UnityEvent onCutsceneFinished;

    private IPharmeeFace _face;
    private Coroutine _routine;
    private bool _subscribed;

    public bool IsPlaying { get; private set; }

    public void SetRunner(ExperimentRunner r)
    {
        Unsubscribe();
        runner = r;
        _face = faceBehaviour as IPharmeeFace;
        Subscribe();
    }

    private void OnEnable() { _face = faceBehaviour as IPharmeeFace; Subscribe(); }
    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed || runner == null) return;
        runner.ExperimentStarted += OnStarted;
        runner.PhaseCompleted += OnPhaseCompleted;
        runner.ExperimentFinished += OnFinished;
        _subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!_subscribed || runner == null) return;
        runner.ExperimentStarted -= OnStarted;
        runner.PhaseCompleted -= OnPhaseCompleted;
        runner.ExperimentFinished -= OnFinished;
        _subscribed = false;
    }

    private void OnStarted(ExperimentModuleDefinition m)
    {
        if (m != null) LoadForModule(m.moduleId);
        Play(intro);
    }

    /// Swap the four cutscenes to the module's set from the library (if present).
    /// Returns true when a complete set was found. Kept public for edit-mode tests.
    public bool LoadForModule(string moduleId)
    {
        if (library == null) return false;
        var set = library.GetSet(moduleId);
        if (set == null) return false;
        if (set.intro != null) intro = set.intro;
        if (set.reagentPrep != null) reagentPrep = set.reagentPrep;
        if (set.success != null) success = set.success;
        if (set.failure != null) failure = set.failure;
        return set.IsComplete;
    }
    private void OnPhaseCompleted(TaskPhase p) { if (p == TaskPhase.ReagentPrep) Play(reagentPrep); }

    private bool _skipNextOutro;

    /// W5.9: a supply-starvation restart records the attempt via Finish(0) but
    /// must NOT play the failure outro over the restart fade — call this right
    /// before that Finish. One-shot; the next finish plays normally.
    public void SkipNextOutro() => _skipNextOutro = true;

    private void OnFinished(ExperimentResult r)
    {
        if (_skipNextOutro) { _skipNextOutro = false; return; }
        Play(SelectOutro(r));
    }

    /// The end cutscene always resolves to something (success or failure variant).
    public CutsceneData SelectOutro(ExperimentResult r) => r.passed ? success : failure;

    public void Play(CutsceneData data)
    {
        if (data == null || data.beats == null || data.beats.Count == 0) return;
        if (_routine != null) StopCoroutine(_routine);
        if (isActiveAndEnabled) _routine = StartCoroutine(PlayRoutine(data));
    }

    private IEnumerator PlayRoutine(CutsceneData data)
    {
        IsPlaying = true;
        onCutsceneStarted?.Invoke();
        for (int i = 0; i < data.beats.Count; i++)
        {
            var b = data.beats[i];
            if (b == null) continue;
            _face?.SetExpression(b.face);
            if (narration != null) narration.Say(b.subtitle, b.seconds);
            yield return new WaitForSeconds(Mathf.Max(0.2f, b.seconds));
        }
        IsPlaying = false;
        onCutsceneFinished?.Invoke();
        _routine = null;
    }

    public void Skip()
    {
        if (_routine != null) StopCoroutine(_routine);
        if (narration != null) narration.SkipNarration();
        IsPlaying = false;
        _routine = null;
        onCutsceneFinished?.Invoke();
    }
}
