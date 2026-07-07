using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TabletGestureController : MonoBehaviour
{
    [SerializeField] private InputActionReference gestureAction;
    [SerializeField] private float activationThreshold = 0.5f;
    [SerializeField] private float cooldownSeconds = 0.5f;
    [SerializeField] private bool triggerOnButtonPress = true;

    public UnityEvent onTabletGesture;

    private float lastTriggerTime = -999f;

    private void OnEnable()
    {
        if (gestureAction != null && gestureAction.action != null)
            gestureAction.action.Enable();
    }

    private void OnDisable()
    {
        if (gestureAction != null && gestureAction.action != null)
            gestureAction.action.Disable();
    }

    private void Update()
    {
        if (gestureAction == null || gestureAction.action == null)
            return;

        if (Time.time - lastTriggerTime < cooldownSeconds)
            return;

        if (triggerOnButtonPress)
        {
            if (gestureAction.action.WasPressedThisFrame())
            {
                TriggerGesture();
            }
            return;
        }

        float value = gestureAction.action.ReadValue<float>();
        if (value >= activationThreshold)
            TriggerGesture();
    }

    private void TriggerGesture()
    {
        lastTriggerTime = Time.time;
        onTabletGesture?.Invoke();
    }
}
