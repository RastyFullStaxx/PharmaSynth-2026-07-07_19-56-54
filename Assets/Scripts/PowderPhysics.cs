using UnityEngine;

public class PowderPhysics : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("The mesh representing the powder inside the container.")]
    public Transform powderMesh; 
    public Renderer powderRenderer;

    [Header("Volume Settings")]
    public float maxVolume = 1000f;
    public float currentPowderVolume = 500f;

    [Header("Chemical Content")]
    public ChemicalData currentChemical;

    void Start()
    {
        UpdateVisuals();
    }

    void Update()
    {
        // Clamp volume just like the liquid system
        currentPowderVolume = Mathf.Clamp(currentPowderVolume, 0, maxVolume);
        UpdateVisuals();
    }

    public void AddPowder(ChemicalData incomingChemical, float amountToAdd)
    {
        if (incomingChemical == null) return;
        if (currentPowderVolume + amountToAdd > maxVolume) return;

        // If container is empty, take on the properties of the incoming powder
        if (currentPowderVolume <= 0.1f)
        {
            currentChemical = incomingChemical;
            
            // Set the color of the powder mesh to match the chemical
            if (powderRenderer != null && currentChemical != null)
            {
                // Assuming standard material; update if using a custom shader property
                powderRenderer.material.color = currentChemical.liquidColor; 
            }
        }

        currentPowderVolume += amountToAdd;
        UpdateVisuals();
    }

    public ChemicalData PourOut(float amountToRemove)
    {
        if (currentPowderVolume <= 0) return null;

        currentPowderVolume -= amountToRemove;
        if (currentPowderVolume < 0) currentPowderVolume = 0;

        UpdateVisuals();
        return currentChemical;
    }

    private void UpdateVisuals()
    {
        if (powderMesh == null) return;

        float fillRatio = currentPowderVolume / maxVolume;

        // Cutoff Logic (Hide if empty)
        bool hasPowder = currentPowderVolume > 1f;
        if (powderMesh.gameObject.activeSelf != hasPowder)
        {
            powderMesh.gameObject.SetActive(hasPowder);
        }

        // Scale the powder pile vertically based on fill ratio
        // (Assuming the mesh's pivot is at its bottom)
        Vector3 newScale = powderMesh.localScale;
        newScale.y = fillRatio; // Adjust this multiplier based on your specific 3D model
        powderMesh.localScale = newScale;
    }
}