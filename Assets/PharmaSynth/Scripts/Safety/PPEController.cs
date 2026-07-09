using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// PPE (lab coat / goggles / gloves) donning at the locker. PPE-worn state is
/// graded (Materials & PPE rubric) and gates lab entry — the door blockers are
/// removed once PPE is on. A mirror/avatar reflection can subscribe to onPPEWorn.
/// Poke/grab an XR interactable on this object to don PPE.
public class PPEController : MonoBehaviour
{
    [SerializeField] private GameObject[] labEntryBlockers;  // e.g. a locked-door collider
    [SerializeField] private GameObject[] wornVisuals;       // coat/gloves shown on the player
    [SerializeField] private bool donOnSelect = true;

    public UnityEvent onPPEWorn;
    public event Action PPEWornChanged;

    public bool PPEWorn { get; private set; }

    private XRBaseInteractable _hooked;

    private void OnEnable()
    {
        if (!donOnSelect) return;
        _hooked = GetComponent<XRBaseInteractable>();
        if (_hooked != null) _hooked.selectEntered.AddListener(OnSelect);
    }

    private void OnDisable()
    {
        if (_hooked != null) _hooked.selectEntered.RemoveListener(OnSelect);
    }

    private void OnSelect(SelectEnterEventArgs _) => DonPPE();

    /// Hook to the locker interaction. Idempotent.
    public void DonPPE()
    {
        if (PPEWorn) return;
        PPEWorn = true;

        if (labEntryBlockers != null)
            foreach (var b in labEntryBlockers) if (b != null) b.SetActive(false);
        if (wornVisuals != null)
            foreach (var v in wornVisuals) if (v != null) v.SetActive(true);

        AudioService.TryPlay("ppe-rustle");
        onPPEWorn?.Invoke();
        PPEWornChanged?.Invoke();
    }

    public void RemovePPE()
    {
        if (!PPEWorn) return;
        PPEWorn = false;
        if (wornVisuals != null)
            foreach (var v in wornVisuals) if (v != null) v.SetActive(false);
        PPEWornChanged?.Invoke();
    }
}
