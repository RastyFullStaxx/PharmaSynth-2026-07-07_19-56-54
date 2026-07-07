using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChemLabelUpdater : MonoBehaviour
{
    public ChemicalData chemicalData;
    public Text uiText;
    public TMP_Text tmpText;

    [Header("Style")]
    public bool applyReadableStyle = true;
    public Color readableColor = new Color(1f, 0.95f, 0.55f);

    void Start()
    {
        if (chemicalData == null)
            return;

        if (uiText != null)
        {
            uiText.text = chemicalData.chemicalName;
            if (applyReadableStyle)
                uiText.color = readableColor;
        }

        if (tmpText != null)
        {
            tmpText.text = chemicalData.chemicalName;
            if (applyReadableStyle)
                tmpText.color = readableColor;
        }

    }
}
