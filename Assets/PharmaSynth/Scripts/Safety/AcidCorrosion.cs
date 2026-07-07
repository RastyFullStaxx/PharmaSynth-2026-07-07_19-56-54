using UnityEngine;
using UnityEngine.Rendering.Universal; // For Decal
using System.Collections;

public class AcidCorrosion : MonoBehaviour
{
    [Header("Corrosion Settings")]
    public float corrosionDuration = 5.0f;
    [Range(0f, 1f)] public float startOpacity = 0.05f;
    [Range(0f, 1f)] public float targetOpacity = 0.95f;

    [Header("Audio Settings")]
    public float initialVolume = 1.0f;
    public float residualVolume = 0.1f; // Volume after burning finishes
    public float audioFadeTime = 2.0f; // How long to fade down

    [Header("References")]
    public DecalProjector decal;
    public AudioSource fizzSound;
    public ParticleSystem smokeParticles;
    public Transform warningUI; //Transform to scale it

    private float timer = 0f;
    private bool isFinished = false;

    void Start()
    {
        // 1. Setup Decal
        if (decal == null) decal = GetComponentInChildren<DecalProjector>();
        if (decal != null) decal.fadeFactor = startOpacity;

        // 2. Setup Audio
        if (fizzSound == null) fizzSound = GetComponentInChildren<AudioSource>();
        if (fizzSound != null)
        {
            fizzSound.volume = initialVolume;
            fizzSound.Play();
        }

        // 3. Setup Smoke
        if (smokeParticles != null)
        {
            smokeParticles.Play();
        }

        // 4. ANIMATION: Start the Pop-Up Effect
        if (warningUI != null)
        {
            warningUI.gameObject.SetActive(true);
            StartCoroutine(AnimateUIPop());
        }
    }

    void Update()
    {
        // PHASE 1: ACTIVE CORROSION
        if (timer < corrosionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / corrosionDuration;

            // Darken the Decal
            if (decal != null)
            {
                decal.fadeFactor = Mathf.Lerp(startOpacity, targetOpacity, t);
            }
        }
        // PHASE 2: FINISHED / FADING OUT
        else if (!isFinished)
        {
            isFinished = true;
            StartCoroutine(FadeOutEffects());
        }
    }

    // Coroutine to animate the UI scaling up with a bounce
    IEnumerator AnimateUIPop()
    {
        float animTime = 0f;
        float duration = 0.5f;
        Vector3 finalScale = warningUI.localScale; // Remember the size you set in Editor

        warningUI.localScale = Vector3.zero; // Start invisible

        while (animTime < duration)
        {
            animTime += Time.deltaTime;
            float t = animTime / duration;
            float curve = t * (1.5f - 0.5f * t);
            float backOut = 2.70158f;
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float t_pop = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);

            warningUI.localScale = finalScale * t_pop;
            yield return null;
        }
        warningUI.localScale = finalScale;
    }

    // Coroutine to handle the smoke stopping and audio fading
    IEnumerator FadeOutEffects()
    {
        //Stop Smoke Emission
        if (smokeParticles != null)
        {
            smokeParticles.Stop();
        }

        //Fade Audio Volume
        if (fizzSound != null)
        {
            float startVol = fizzSound.volume;
            float fadeTimer = 0f;

            while (fadeTimer < audioFadeTime)
            {
                fadeTimer += Time.deltaTime;
                float t = fadeTimer / audioFadeTime;

                fizzSound.volume = Mathf.Lerp(startVol, residualVolume, t);
                yield return null;
            }

            fizzSound.volume = residualVolume;

            // Optional: If residual is 0, just stop it to save CPU
            if (residualVolume <= 0.01f) fizzSound.Stop();
        }
    }
}