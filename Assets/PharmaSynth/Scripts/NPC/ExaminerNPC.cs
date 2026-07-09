using UnityEngine;

/// Dr. Jimenez — the assessment-mode examiner (plan §3.4). In experiments flagged
/// assessmentMode he observes and gives NO hints (Pharmee also stays quiet); otherwise
/// he's dormant. Reacts only at the end (records the outcome). The behaviour is code;
/// the rigged model is an art-pass item — this drives whatever visual is assigned.
public class ExaminerNPC : MonoBehaviour
{
    public enum ExaminerState { Dormant, Observing, Recording }

    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private MonoBehaviour faceOrAnimatorHook; // optional visual, wired in art pass

    private bool _subscribed;

    public ExaminerState State { get; private set; } = ExaminerState.Dormant;
    public bool IsObserving => State == ExaminerState.Observing;

    /// Pure predicate — does this module put the examiner on watch?
    public static bool ShouldObserve(ExperimentModuleDefinition m) => m != null && m.assessmentMode;

    public void SetRunner(ExperimentRunner r) { Unsubscribe(); runner = r; Subscribe(); }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed || runner == null) return;
        runner.ExperimentStarted += OnStarted;
        runner.ExperimentFinished += OnFinished;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || runner == null) return;
        runner.ExperimentStarted -= OnStarted;
        runner.ExperimentFinished -= OnFinished;
        _subscribed = false;
    }

    private void OnStarted(ExperimentModuleDefinition m)
        => State = ShouldObserve(m) ? ExaminerState.Observing : ExaminerState.Dormant;

    private void OnFinished(ExperimentResult r)
    {
        if (State == ExaminerState.Observing) State = ExaminerState.Recording;
        // (art pass: play a clipboard-note animation here; the grade is recorded by the grader)
    }
}
