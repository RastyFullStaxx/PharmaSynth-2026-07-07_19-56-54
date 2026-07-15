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
        var prev = MakeButton(parent, submit, "QuizPrevButton", "< Back", -0.16f);
        var next = MakeButton(parent, submit, "QuizNextButton", "Next >", 0.16f);

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

    /// Clone the Submit button for consistent styling, offset sideways from it.
    static Button MakeButton(Transform parent, Button template, string name, string label, float xOffset)
    {
        var existing = parent != null ? parent.Find(name) : null;
        GameObject go = existing != null ? existing.gameObject
                                         : Object.Instantiate(template.gameObject, parent);
        go.name = name;
        if (existing == null)
        {
            // Sit beside Submit, on the same plane.
            go.transform.localRotation = template.transform.localRotation;
            go.transform.localScale = template.transform.localScale;
            var p = template.transform.localPosition;
            go.transform.localPosition = new Vector3(p.x + xOffset, p.y, p.z);
        }
        go.SetActive(true);
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
        return go.GetComponentInChildren<Button>(true);
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
