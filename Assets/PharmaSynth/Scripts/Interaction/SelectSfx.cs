using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteractor = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;

/// Plays a SoundBank key when this interactor selects something — used for the
/// station snap sockets ("socket-snap" as the prop clicks into place).
public class SelectSfx : MonoBehaviour
{
    [SerializeField] private string key = "socket-snap";

    private XRInteractor _interactor;

    void Awake() { if (_interactor == null) Bind(GetComponent<XRInteractor>(), key); }

    public void Bind(XRInteractor interactor, string soundKey)
    {
        if (_interactor != null) _interactor.selectEntered.RemoveListener(OnSelect);
        _interactor = interactor; key = soundKey;
        if (_interactor != null) _interactor.selectEntered.AddListener(OnSelect);
    }

    void OnDestroy() { if (_interactor != null) _interactor.selectEntered.RemoveListener(OnSelect); }

    private void OnSelect(SelectEnterEventArgs _) => AudioService.TryPlay(key);
}
