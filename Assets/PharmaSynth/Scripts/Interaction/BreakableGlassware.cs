using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableGlassware : MonoBehaviour
{
    [Header("Break Conditions")]
    [SerializeField] private float breakImpactThreshold = 2.8f;
    [SerializeField] private bool breakOnFloorCollision = true;
    [SerializeField] private string floorTag = "Floor";

    [Header("Break Effects")]
    [SerializeField] private GameObject brokenGlassPrefab;
    [SerializeField] private ParticleSystem shatterParticles;
    [SerializeField] private AudioSource shatterAudio;
    [SerializeField] private bool disableOriginalOnBreak = true;

    private bool isBroken;

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken)
            return;

        bool floorCheckPassed = !breakOnFloorCollision || collision.collider.CompareTag(floorTag);
        if (!floorCheckPassed)
            return;

        if (collision.relativeVelocity.magnitude < breakImpactThreshold)
            return;

        Break();
    }

    public void Break()
    {
        if (isBroken)
            return;

        isBroken = true;

        if (brokenGlassPrefab != null)
            Instantiate(brokenGlassPrefab, transform.position, transform.rotation);

        if (shatterParticles != null)
            shatterParticles.Play();

        if (shatterAudio != null)
            shatterAudio.Play();

        ExperimentFlowManager.Instance?.MarkGlasswareDropped(gameObject.name);

        if (disableOriginalOnBreak)
            gameObject.SetActive(false);
    }
}
