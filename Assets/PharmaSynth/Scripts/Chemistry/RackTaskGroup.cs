using System.Collections.Generic;
using UnityEngine;

/// Pure rules for a rack of tubes that share one step (2026-07-16).
///
/// Manuscript Exp 2 runs the same test across a SET of tubes — five alcohols for
/// the enol test, three butyl alcohols beside a negative control, acetone beside
/// acetaldehyde, hydrolysed beside unhydrolysed aspirin. In every case **the
/// comparison across the tubes IS the lesson**, so the step is only done when
/// every tube that the step names has had its reagent.
///
/// This exists because LiquidTaskBinding is per-VESSEL: give five tubes a binding
/// for the same task and the first tube to hit its threshold completes it,
/// quietly making the other four optional and throwing the lesson away. The rack
/// members are authored completesTask:false (so their pours are expected and
/// accumulate, but completion is not theirs) and the group calls it in.
public static class RackMath
{
    /// The step is done only when EVERY member tube is ready. A rack with no
    /// members is never ready — an empty group must not auto-complete a step.
    public static bool AllReady(int readyTubes, int memberTubes)
        => memberTubes > 0 && readyTubes >= memberTubes;

    /// Progress copy while the set fills in — the count is the point, so show it.
    public static string ProgressLabel(int readyTubes, int memberTubes)
        => "tube " + readyTubes + " of " + memberTubes;

    /// A tube left DELIBERATELY empty (Exp 2's negative control) is simply not a
    /// member of that step's group: it declares no binding for the task, so it is
    /// never counted, and pouring into it is a genuine wrong-reagent mistake —
    /// which is exactly how the control teaches experimental design.
    public static int CountReady(IReadOnlyList<LiquidTaskBinding> members, string taskId)
    {
        if (members == null) return 0;
        int n = 0;
        for (int i = 0; i < members.Count; i++)
            if (members[i] != null && members[i].ReadyFor(taskId)) n++;
        return n;
    }
}

/// Completes one task when every tube in its rack has had what the step asked of
/// it. Thin driver over RackMath; poll-based so it needs no event plumbing into
/// LiquidTaskBinding (which already tracks its own readiness).
public class RackTaskGroup : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private string taskId;
    [SerializeField] private List<LiquidTaskBinding> members = new List<LiquidTaskBinding>();

    private bool _fired;
    private int _lastReady = -1;

    public int MemberCount => members.Count;
    public string TaskId => taskId;

    /// Edit-mode / builder seam (AddComponent fires no Awake in edit mode).
    public void Bind(ExperimentRunner r, string task, List<LiquidTaskBinding> tubes)
    {
        runner = r; taskId = task;
        members = tubes ?? new List<LiquidTaskBinding>();
        _fired = false; _lastReady = -1;
    }

    /// Pure check, exposed so the suite can drive it without a frame loop.
    public bool ShouldFire()
        => !_fired && runner != null && !string.IsNullOrEmpty(taskId)
           && runner.Graph != null && !runner.Graph.IsComplete(taskId)
           && RackMath.AllReady(RackMath.CountReady(members, taskId), members.Count);

    void Update()
    {
        if (!Application.isPlaying || _fired) return;
        if (runner == null || runner.Graph == null || string.IsNullOrEmpty(taskId)) return;

        int ready = RackMath.CountReady(members, taskId);
        if (ready != _lastReady && ready > 0 && ready < members.Count)
        {
            // Tell the player the SET is the step, so a finished tube doesn't read
            // as a finished step ("why didn't it tick?").
            FloatingText.Show(RackMath.ProgressLabel(ready, members.Count),
                              transform.position + Vector3.up * 0.25f,
                              new Color(0.6f, 0.85f, 1f), 0.9f);
        }
        _lastReady = ready;

        if (ShouldFire()) { _fired = true; runner.CompleteTask(taskId); }
    }
}
