using UnityEngine;

[CreateAssetMenu(fileName = "NewReaction", menuName = "Chemistry/Reaction Rule")]
public class ReactionRule : ScriptableObject
{
    [Header("Reactants")]
    public ChemicalData inputChemicalA;
    public ChemicalData inputChemicalB;

    [Header("Result")]
    public ChemicalData resultLiquid;      // The resulting liquid (e.g., Water)

    [Header("Precipitate (Optional)")]
    public ChemicalData resultPrecipitate; // The solid/ppt (e.g., Cu(OH)2)
    public bool hasPrecipitate;            // Check this if it makes a solid
}