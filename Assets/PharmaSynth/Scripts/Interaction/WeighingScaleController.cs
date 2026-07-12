using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WeighingScaleController : MonoBehaviour
{
    [SerializeField] private TMP_Text measurementText;
    [SerializeField] private string unit = "g";
    [SerializeField] private float interpolationSpeed = 20f;
    [SerializeField] private float targetReachedEpsilon = 0.05f;

    [SerializeField] private float currentMass;
    [SerializeField] private float targetMass;

    public UnityEvent onTargetReached;

    private bool targetEventSent;

    public float CurrentMass => currentMass;

    /// Builder seam (W5.8): wire the display TMP at runtime.
    public void Bind(TMP_Text display) => measurementText = display;

    private void Update() => Tick(Time.deltaTime);

    /// One interpolation step (public so the suite can drive it deterministically).
    public void Tick(float dt)
    {
        currentMass = Mathf.MoveTowards(currentMass, targetMass, interpolationSpeed * Mathf.Max(0f, dt));
        UpdateDisplay();

        if (!targetEventSent && Mathf.Abs(currentMass - targetMass) <= targetReachedEpsilon)
        {
            targetEventSent = true;
            onTargetReached?.Invoke();
        }
    }

    public void SetTargetMass(float newTarget)
    {
        targetMass = Mathf.Max(0f, newTarget);
        targetEventSent = false;
    }

    public void AddMass(float delta)
    {
        SetTargetMass(targetMass + delta);
    }

    public void RemoveMass(float delta)
    {
        SetTargetMass(targetMass - delta);
    }

    private void UpdateDisplay()
    {
        if (measurementText == null)
            return;

        measurementText.text = currentMass.ToString("0.00") + " " + unit;
    }
}
