using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class XRBottleUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel; // The UI to show/hide
    [SerializeField] private TMP_Text volumeText;  // Text inside the UI

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private LiquidData liquidData;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        liquidData = GetComponent<LiquidData>();
        
        // Ensure UI is hidden at start
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // 1. Activate the panel
        if (menuPanel != null) menuPanel.SetActive(true);

        // 2. Retrieve data and update UI
        if (liquidData != null && volumeText != null)
        {
            volumeText.text = $"{liquidData.liquidName}: {liquidData.currentVolume}ml";
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Hide the panel when released
        if (menuPanel != null) menuPanel.SetActive(false);
    }
}