using UnityEngine;

public class MoveInstructionsOnTilt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handToWatch;
    [Tooltip("Assign the instruction popup/panel here. Do not leave this empty if this script is attached to a menu button.")]
    [SerializeField] private Transform instructionsRoot;
    [SerializeField] private Component handInteractor;
    [Tooltip("Usually the XR camera or Main Camera. If empty, the script will use Camera.main.")]
    [SerializeField] private Transform viewer;

    [Header("Optional Name Lookup")]
    [SerializeField] private string handToWatchName;
    [SerializeField] private string instructionsName;
    [SerializeField] private string handInteractorObjectName;
    [SerializeField] private string viewerName;

    [Header("Tilt")]
    [SerializeField, Range(0f, 180f)] private float showAtAngle = 90f;
    [SerializeField, Range(0f, 30f)] private float resetHysteresis = 8f;
    [SerializeField] private bool requireEmptyHand = true;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool requireResetBeforeFirstShow = true;

    [Header("View Placement")]
    [SerializeField] private bool moveInFrontOfViewer = true;
    [SerializeField] private bool followViewerWhileVisible;
    [SerializeField, Min(0.1f)] private float viewerDistance = 1.5f;
    [SerializeField] private Vector3 viewerOffset = new Vector3(0f, -0.15f, 0f);
    [SerializeField] private bool faceViewer = true;

    private CanvasGroup canvasGroup;
    private bool isShowing;
    private bool waitingForHandReset;

    private void Awake()
    {
        ResolveMissingReferences();

        if (instructionsRoot == null)
        {
            Debug.LogWarning(
                $"{nameof(MoveInstructionsOnTilt)} on '{name}' has no Instructions Root assigned. " +
                "Assign the instruction popup/panel, not the menu button.",
                this);
            enabled = false;
            return;
        }

        canvasGroup = instructionsRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = instructionsRoot.gameObject.AddComponent<CanvasGroup>();

        if (hideOnStart)
            HideInstructions();

        waitingForHandReset = requireResetBeforeFirstShow;
    }

    private void ResolveMissingReferences()
    {
        if (handToWatch == null && !string.IsNullOrWhiteSpace(handToWatchName))
            handToWatch = FindSceneTransform(handToWatchName);

        if (instructionsRoot == null && !string.IsNullOrWhiteSpace(instructionsName))
            instructionsRoot = FindSceneTransform(instructionsName);

        if (handInteractor == null && !string.IsNullOrWhiteSpace(handInteractorObjectName))
            handInteractor = FindFirstComponentOnObject(handInteractorObjectName);

        if (viewer == null && !string.IsNullOrWhiteSpace(viewerName))
            viewer = FindSceneTransform(viewerName);

        if (viewer == null && Camera.main != null)
            viewer = Camera.main.transform;

        if (viewer == null)
            viewer = FindFirstSceneCamera();
    }

    private void LateUpdate()
    {
        if (handToWatch == null || instructionsRoot == null || canvasGroup == null)
            return;

        float tiltAngle = Vector3.Angle(Vector3.up, handToWatch.up);
        bool handHasReset = tiltAngle <= showAtAngle - resetHysteresis;

        if (handHasReset)
            waitingForHandReset = false;

        if (isShowing && followViewerWhileVisible)
            MoveInstructionsInFrontOfViewer();

        if (!waitingForHandReset && tiltAngle >= showAtAngle && CanShowForHandState())
        {
            ToggleInstructions();
            waitingForHandReset = true;
        }
    }

    public void ShowInstructions()
    {
        MoveInstructionsInFrontOfViewer();
        SetInstructionsVisible(true);
        isShowing = true;
    }

    public void CloseInstructions()
    {
        HideInstructions();
        waitingForHandReset = true;
    }

    private void HideInstructions()
    {
        SetInstructionsVisible(false);
        isShowing = false;
    }

    private void ToggleInstructions()
    {
        if (isShowing)
            HideInstructions();
        else
            ShowInstructions();
    }

    private void SetInstructionsVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        bool canToggleRootObject = instructionsRoot != null && instructionsRoot.gameObject != gameObject;
        if (canToggleRootObject && instructionsRoot.gameObject.activeSelf != visible)
            instructionsRoot.gameObject.SetActive(visible);

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void MoveInstructionsInFrontOfViewer()
    {
        if (!moveInFrontOfViewer || instructionsRoot == null || viewer == null)
            return;

        Vector3 targetPosition = viewer.position + viewer.forward * viewerDistance;
        targetPosition += viewer.TransformDirection(viewerOffset);
        instructionsRoot.position = targetPosition;

        if (faceViewer)
        {
            Vector3 directionToViewer = instructionsRoot.position - viewer.position;
            directionToViewer.y = 0f;

            if (directionToViewer.sqrMagnitude > 0.0001f)
                instructionsRoot.rotation = Quaternion.LookRotation(directionToViewer.normalized, Vector3.up);
        }
    }

    private bool CanShowForHandState()
    {
        return !requireEmptyHand || handInteractor == null || !InteractorHasSelection(handInteractor);
    }

    private static bool InteractorHasSelection(Component interactor)
    {
        System.Type type = interactor.GetType();

        System.Reflection.PropertyInfo hasSelectionProperty = type.GetProperty("hasSelection");
        if (hasSelectionProperty != null && hasSelectionProperty.PropertyType == typeof(bool))
            return (bool)hasSelectionProperty.GetValue(interactor);

        System.Reflection.PropertyInfo selectedProperty = type.GetProperty("interactablesSelected");
        object selectedValue = selectedProperty?.GetValue(interactor);
        if (selectedValue is System.Collections.ICollection selectedCollection)
            return selectedCollection.Count > 0;

        return false;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.gameObject.scene.IsValid() && sceneTransform.name == objectName)
                return sceneTransform;
        }

        Debug.LogWarning($"MoveInstructionsOnTilt could not find a scene object named '{objectName}'.");
        return null;
    }

    private static Component FindFirstComponentOnObject(string objectName)
    {
        Transform sceneTransform = FindSceneTransform(objectName);
        if (sceneTransform == null)
            return null;

        Component[] components = sceneTransform.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component != null && component.GetType().Name.Contains("Interactor"))
                return component;
        }

        Debug.LogWarning($"MoveInstructionsOnTilt found '{objectName}', but it has no component with 'Interactor' in its type name.");
        return null;
    }

    private static Transform FindFirstSceneCamera()
    {
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera sceneCamera in cameras)
        {
            if (sceneCamera != null && sceneCamera.gameObject.scene.IsValid() && sceneCamera.isActiveAndEnabled)
                return sceneCamera.transform;
        }

        return null;
    }
}
