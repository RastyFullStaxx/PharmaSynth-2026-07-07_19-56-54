using UnityEngine;

public class TestReaction : MonoBehaviour
{
}

// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TestReaction : MonoBehaviour
// {
//     public LiquidPhysics targetBeaker;
//     public ChemicalData chemicalToPour;
//     public float amount = 50f;
//     public InputActionReference testbutton;

//     private void OnEnable()
//     {
//         if (testbutton != null && testbutton.action != null)
//             testbutton.action.Enable();
//     }

//    void Update()
// {
//     if (testbutton == null)
//     {
//         Debug.LogError("[TestReaction] testbutton is NULL");
//         return;
//     }

//     if (testbutton.action == null)
//     {
//         Debug.LogError("[TestReaction] testbutton.action is NULL");
//         return;
//     }

//     if (testbutton.action.WasPressedThisFrame())
//     {
//         Debug.Log("Button Pressed");

//         targetBeaker.AddLiquid(chemicalToPour, amount);

//         Debug.Log($"Poured {amount}ml of {chemicalToPour.chemicalName}");
//     }
// }

//     private void OnDisable()
//     {
//         if (testbutton != null && testbutton.action != null)
//             testbutton.action.Disable();
//     }
// }