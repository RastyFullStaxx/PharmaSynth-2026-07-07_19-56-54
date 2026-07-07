// 12/14/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TunnelingEffectController : MonoBehaviour
{
    public Volume postProcessingVolume;
    public float maxVignetteIntensity = 0.5f;
    public float movementThreshold = 0.1f;
    public float fadeSpeed = 2.0f;

    private Vignette vignette;
    private CharacterController characterController;

    private void Start()
    {
        if (postProcessingVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = 0f;
        }
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (characterController != null && vignette != null)
        {
            float speed = characterController.velocity.magnitude;

            if (speed > movementThreshold)
            {
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, maxVignetteIntensity, Time.deltaTime * fadeSpeed);
            }
            else
            {
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, Time.deltaTime * fadeSpeed);
            }
        }
    }
}