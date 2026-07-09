using System.Collections;
using TMPro;
using UnityEngine;

/// Screen-bottom dialogue bar (storyboard style): Pharmee's portrait + the line he
/// is currently speaking, mirrored from NPCNarrationController so the player can
/// read him without looking at him. Visible ONLY while a line is live; fades out
/// smoothly when the line ends.
public class HudDialogueBar : MonoBehaviour
{
    [SerializeField] private NPCNarrationController narration;
    [SerializeField] private GameObject barRoot;      // toggled with the line
    [SerializeField] private TMP_Text speakerText;    // "Pharmee"
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private float fadeSeconds = 0.5f;

    private bool _subscribed;
    private CanvasGroup _group;
    private Coroutine _fade;

    /// Edit-mode/test binding.
    public void Bind(NPCNarrationController n, GameObject root, TMP_Text speaker, TMP_Text line)
    {
        Unsubscribe();
        narration = n; barRoot = root; speakerText = speaker; lineText = line;
        Subscribe();
        if (barRoot != null) barRoot.SetActive(false);
    }

    private void OnEnable()
    {
        Subscribe();
        if (barRoot != null && (narration == null || !narration.IsSpeaking))
            barRoot.SetActive(false);
    }

    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed || narration == null) return;
        narration.LineStarted += HandleLineStarted;
        narration.LineEnded += HandleLineEnded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || narration == null) return;
        narration.LineStarted -= HandleLineStarted;
        narration.LineEnded -= HandleLineEnded;
        _subscribed = false;
    }

    private CanvasGroup Group()
    {
        if (_group == null && barRoot != null)
        {
            _group = barRoot.GetComponent<CanvasGroup>();
            if (_group == null) _group = barRoot.AddComponent<CanvasGroup>();
        }
        return _group;
    }

    /// Public for headless tests.
    public void HandleLineStarted(string line, float seconds)
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        if (lineText != null) lineText.text = line;
        if (speakerText != null && string.IsNullOrEmpty(speakerText.text)) speakerText.text = "Pharmee";
        var g = Group();
        if (g != null) g.alpha = 1f;
        if (barRoot != null) barRoot.SetActive(true);
    }

    public void HandleLineEnded()
    {
        // Runtime: fade out smoothly; edit mode/tests: hide immediately.
        if (Application.isPlaying && isActiveAndEnabled && barRoot != null && barRoot.activeSelf)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeOutRoutine());
        }
        else HideNow();
    }

    private void HideNow()
    {
        if (barRoot != null) barRoot.SetActive(false);
        if (lineText != null) lineText.text = string.Empty;
        var g = Group();
        if (g != null) g.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine()
    {
        var g = Group();
        float t = 0f;
        float dur = Mathf.Max(0.05f, fadeSeconds);
        while (t < dur)
        {
            t += Time.deltaTime;
            if (g != null) g.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        _fade = null;
        HideNow();
    }
}
