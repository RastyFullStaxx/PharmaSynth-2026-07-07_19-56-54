using UnityEngine;

public class SaltBurn : MonoBehaviour
{
    [Header("Settings")]
    public float burnDuration = 3.0f; // Total seconds needed to burn completely

    [Header("References")]
    public ParticleSystem flame;

    // Internal state
    private float timer = 0f;
    private bool isBurning = false;

    void Start()
    {
        // Ensure flame is off at start
        if (flame != null) flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        // Only increase timer if currently burning
        if (isBurning)
        {
            timer += Time.deltaTime;

            // Check if we reached the limit
            if (timer >= burnDuration)
            {
                FinishBurning();
            }
        }
    }

    void FinishBurning()
    {
        // 1. Stop the fire effects just in case
        if (flame) flame.Stop();

        // 2. Disable this entire Salt GameObject
        // This hides the mesh, removes the collider, and stops this script immediately.
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Start burning if we hit the spirit flame
        if (other.CompareTag("spiritflame"))
        {
            isBurning = true;
            if (flame) flame.Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Pause burning if we leave the flame
        if (other.CompareTag("spiritflame"))
        {
            isBurning = false;
            if (flame) flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}