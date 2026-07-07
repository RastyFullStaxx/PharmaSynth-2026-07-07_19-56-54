using UnityEngine;

public class TVRepositionController : MonoBehaviour
{
    [SerializeField] private Transform tvTransform;
    [SerializeField] private Vector3 targetLocalPosition;
    [SerializeField] private Vector3 targetLocalEulerAngles;
    [SerializeField] private bool applyOnStart = true;

    private void Start()
    {
        if (applyOnStart)
            ApplyReposition();
    }

    public void ApplyReposition()
    {
        if (tvTransform == null)
            return;

        tvTransform.localPosition = targetLocalPosition;
        tvTransform.localRotation = Quaternion.Euler(targetLocalEulerAngles);
    }
}
