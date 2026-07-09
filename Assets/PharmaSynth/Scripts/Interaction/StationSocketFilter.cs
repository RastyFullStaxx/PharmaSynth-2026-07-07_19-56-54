using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// Select filter for a station's snap socket (§2 sockets): the socket accepts
/// only the station's required item, so the beaker clicks onto the heat pad
/// but a random spatula bounces off. Empty requiredItemId = accept anything.
public class StationSocketFilter : MonoBehaviour, IXRSelectFilter
{
    public string requiredItemId = "";

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        => Matches(requiredItemId, interactable != null ? interactable.transform.GetComponentInParent<LabItem>() : null);

    /// Pure matcher so the self-tests pin the policy.
    public static bool Matches(string requiredId, LabItem item)
        => string.IsNullOrEmpty(requiredId) || (item != null && item.itemId == requiredId);
}
