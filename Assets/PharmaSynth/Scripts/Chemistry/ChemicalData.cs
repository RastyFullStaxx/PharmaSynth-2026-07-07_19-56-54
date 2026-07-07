using UnityEngine;

[CreateAssetMenu(fileName = "ChemicalData", menuName = "Chemistry/ChemicalData")]
public class ChemicalData : ScriptableObject
{
    [Header("Identity")]
    public string chemicalName = "Water";

    [Header("Visuals")]
    [ColorUsage(true, true)]
    public Color liquidColor;

    [ColorUsage(true, true)]
    public Color liquidTopColor;

    [Range(0.0f, 1.0f)]
    public float sceneColourAmount;
    [Range(0f, 1f)]
    public float viscosity = 0.5f;
    [Header("Properties")]
    public bool isDangerous = false; // For spilling logic later
}