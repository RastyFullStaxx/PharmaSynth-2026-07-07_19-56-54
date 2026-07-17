using UnityEngine;

/// Compresses a real-world wait the player can't sit through (user 2026-07-17:
/// "after the mixture requirement is set, the screen fades black for 2 s, and
/// when it returns a success text says the time has passed and it's done. Set
/// this for all procedures across all experiments that need lengthy time").
///
/// A task authored longProcess=true fades the screen to black on completion,
/// holds, then fades back in with a "time has passed" message. Zone-free and
/// experiment-agnostic: it keys off the task flag, so any module's week-long
/// fermentation / overnight dry / hour-long crystallisation reuses it for free.
public class TimeSkipController : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private float holdSeconds = 2f;
    [SerializeField] private float fadeSeconds = 0.6f;
    private bool _subscribed;

    /// Pure (suite-pinned): does this task trigger a time skip, and what does it say?
    public static bool IsTimeSkip(ExperimentTask t) => t != null && t.longProcess;
    public static string MessageFor(ExperimentTask t)
    {
        if (t == null) return "";
        return string.IsNullOrWhiteSpace(t.longProcessMessage)
            ? "Time passes… the process is complete."
            : t.longProcessMessage.Trim();
    }

    private void Awake() { if (runner == null) runner = FindAnyObjectByType<ExperimentRunner>(); Subscribe(); }
    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    /// Builder/test seam.
    public void Bind(ExperimentRunner r) { Unsubscribe(); runner = r; Subscribe(); }

    private void Subscribe()
    {
        if (_subscribed || runner == null) return;
        runner.TaskCompleted += OnTaskCompleted;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        if (runner != null) runner.TaskCompleted -= OnTaskCompleted;
        _subscribed = false;
    }

    private void OnTaskCompleted(ExperimentTask t)
    {
        if (!IsTimeSkip(t) || !Application.isPlaying) return;
        string msg = MessageFor(t);

        if (ScreenFader.Instance != null && ScreenFader.Instance.isActiveAndEnabled)
        {
            // Fade to black, HOLD the darkness, then fade back and announce.
            ScreenFader.Instance.FadeOut(fadeSeconds, () =>
                Invoke(nameof(ReturnFromSkip), Mathf.Max(0f, holdSeconds)));
            _pendingMessage = msg;
        }
        else
        {
            // No fader (tests / menus): just announce, so the beat isn't lost.
            Announce(msg);
        }
    }

    private string _pendingMessage;
    private void ReturnFromSkip()
    {
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeIn(fadeSeconds);
        Announce(_pendingMessage);
        _pendingMessage = null;
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
