using UnityEngine;

public class EquipmentLabelVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject[] labels;
    [SerializeField] private bool hideOnStart = true;

    private void Start()
    {
        if (hideOnStart)
            SetLabelsVisible(false);
    }

    public void SetLabelsVisible(bool visible)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
                labels[i].SetActive(visible);
        }
    }
}
