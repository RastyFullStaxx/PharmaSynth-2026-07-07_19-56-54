using UnityEngine;

/// An ignitable Bunsen/alcohol burner (W5.8: matches finally DO something).
/// Bring a lit matchstick to the burner and it lights — a looping flame +
/// point light; Heat stations whose required prop is a burner only advance
/// their sim while it is LIT (ZoneSimStation ignition gate).
public class BurnerController : MonoBehaviour
{
    public const float MatchIgniteDistance = 0.18f;

    private bool _lit;
    private float _nextScan;
    private GameObject _flame;

    public bool IsLit => _lit;

    /// Pure ignition predicate (suite-pinned).
    public static bool ShouldIgnite(bool burnerLit, bool matchLit, float distance)
        => !burnerLit && matchLit && distance <= MatchIgniteDistance;

    private void Update()
    {
        if (_lit || !Application.isPlaying) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.25f;
        foreach (var match in FindObjectsByType<Matchstick>(FindObjectsSortMode.None))
        {
            if (ShouldIgnite(_lit, match.IsLit, Vector3.Distance(transform.position, match.transform.position)))
            {
                Ignite();
                break;
            }
        }
    }

    public void Ignite()
    {
        if (_lit) return;
        _lit = true;
        EffectVfx.FlamePop(FlamePos());
        AudioService.TryPlayAt("burner-ignite", transform.position);
        if (Application.isPlaying) BuildFlame();
    }

    public void Extinguish()
    {
        _lit = false;
        if (_flame != null) Destroy(_flame);
    }

    private Vector3 FlamePos()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return transform.position + Vector3.up * 0.12f;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return new Vector3(b.center.x, b.max.y + 0.01f, b.center.z);
    }

    /// Small looping procedural flame + warm light at the burner top.
    private void BuildFlame()
    {
        _flame = new GameObject("BurnerFlame");
        _flame.transform.SetParent(transform, true);
        _flame.transform.position = FlamePos();

        var ps = _flame.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.35f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.2f, 0.85f), new Color(0.4f, 0.65f, 1f, 0.8f));
        main.maxParticles = 60;
        var em = ps.emission; em.rateOverTime = 45f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.012f;
        sh.rotation = new Vector3(-90f, 0f, 0f);   // upward
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = EffectVfx.ParticleMaterial();
        r.sortingOrder = 10;
        ps.Play();

        var light = _flame.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.25f);
        light.range = 0.8f;
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;
    }
}
