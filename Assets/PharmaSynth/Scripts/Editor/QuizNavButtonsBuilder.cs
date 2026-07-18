#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// Add "< Back" / "Next >" buttons to the quiz tablet (user 2026-07-15: "so users
/// can review their answers before submitting"). Clones the existing Submit button
/// so styling matches, wires them to PostLabController.PreviousQuestion/NextQuestion,
/// assigns the controller's prevButton/nextButton refs (which grey out at the ends),
/// and makes sure the quiz canvas is XR-ray clickable. Idempotent.
public static class QuizNavButtonsBuilder
{
    [MenuItem("Tools/PharmaSynth/Build Quiz Back-Next Buttons")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[QuizNav] exit Play mode first."); return; }

        var post = Object.FindFirstObjectByType<PostLabController>(FindObjectsInactive.Include);
        if (post == null) { Debug.LogError("[QuizNav] PostLabController not found."); return; }

        var so = new SerializedObject(post);
        var submit = so.FindProperty("submitButton")?.objectReferenceValue as Button;
        if (submit == null) { Debug.LogError("[QuizNav] submitButton not wired — nothing to clone for styling."); return; }

        var parent = submit.transform.parent;
        // Offset by the button's own WIDTH, in CANVAS units — a hardcoded 0.16 was
        // ~zero on a canvas whose buttons are hundreds of units wide, so Back and
        // Next landed exactly on top of each other (user 2026-07-15).
        var prev = MakeButton(parent, submit, "QuizPrevButton", "< Back", -1f);
        var next = MakeButton(parent, submit, "QuizNextButton", "Next >", +1f);

        // The tablet is the quiz — say so (user 2026-07-15).
        foreach (var t in post.GetComponentsInChildren<TMP_Text>(true))
            if (t != null && t.text != null && t.text.Contains("Data Sheet & Documentation"))
            { t.text = "Quiz"; EditorUtility.SetDirty(t); }

        Wire(prev, post.PreviousQuestion);
        Wire(next, post.NextQuestion);

        so.FindProperty("prevButton").objectReferenceValue = prev;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.ApplyModifiedPropertiesWithoutUndo();

        // The XR ray needs a tracked raycaster to click them.
        var canvas = post.GetComponentInChildren<Canvas>(true);
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() != null
            && canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        EditorUtility.SetDirty(post);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("<color=#4CD07D>[QuizNav] '< Back' and 'Next >' added to the quiz tablet — step through the "
                  + "questions to review answers before submitting. Your picked option is highlighted.</color>");
    }

    /// Clone the Submit button for consistent styling and sit it beside Submit.
    /// `dir` is -1 (left) or +1 (right); the step is the button's own width, so the
    /// three never overlap regardless of the canvas's unit scale. Re-running ALWAYS
    /// re-lays them out — an earlier build left them stacked, so a "respect the
    /// existing position" rule would preserve that bug forever.
    static Button MakeButton(Transform parent, Button template, string name, string label, float dir)
    {
        var existing = parent != null ? parent.Find(name) : null;
        GameObject go = existing != null ? existing.gameObject
                                         : Object.Instantiate(template.gameObject, parent);
        go.name = name;

        var trt = template.GetComponent<RectTransform>();
        var rt = go.GetComponent<RectTransform>();
        if (trt != null && rt != null)
        {
            rt.anchorMin = trt.anchorMin;
            rt.anchorMax = trt.anchorMax;
            rt.pivot = trt.pivot;
            rt.sizeDelta = trt.sizeDelta;
            rt.localRotation = trt.localRotation;
            rt.localScale = trt.localScale;
            var prt = parent as RectTransform;
            float step = StepFor(trt.rect.width, prt != null ? prt.rect.width : 0f, trt.anchoredPosition.x);
            rt.anchoredPosition = trt.anchoredPosition + new Vector2(dir * step, 0f);
        }
        else if (existing == null)   // non-RectTransform fallback
        {
            go.transform.localRotation = template.transform.localRotation;
            go.transform.localScale = template.transform.localScale;
            go.transform.localPosition = template.transform.localPosition;
        }
        go.SetActive(true);
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
        return go.GetComponentInChildren<Button>(true);
    }

    /// How far from Submit each side button sits, in canvas units — CONTAINED
    /// (user 2026-07-19: "contain the next and prev button, currently it's going
    /// out of bounds"). The old flat 1.12x button-width step overflowed any panel
    /// narrower than ~3.4 buttons: on the real 820-wide quiz panel a 300-wide
    /// button stepped 336, putting its outer edge at 486 against a 410 half-width
    /// — ~76 units off each side, exactly as the playtest screenshot showed.
    /// Clamped so the button's OUTER edge stays inside the parent with a 4%
    /// margin, and Submit's own offset is respected (it need not be centred).
    /// parentWidth &lt;= 0 (unresolvable rect) keeps the preferred step. Pure + pinned.
    public static float StepFor(float buttonWidth, float parentWidth, float submitOffsetX)
    {
        float w = Mathf.Max(1f, buttonWidth);
        float preferred = w * 1.12f;
        if (parentWidth <= 1f) return preferred;
        float maxStep = (parentWidth * 0.5f) - (w * 0.5f) - (parentWidth * 0.04f) - Mathf.Abs(submitOffsetX);
        return maxStep > 0f ? Mathf.Min(preferred, maxStep) : preferred;
    }

    /// Clear cloned listeners, then wire this button to the given method.
    static void Wire(Button b, UnityEngine.Events.UnityAction call)
    {
        if (b == null) return;
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddVoidPersistentListener(b.onClick, call);
    }
}
#endif
