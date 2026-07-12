using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// A grabbable matchstick (manuscript: combustion/flammability tests, Exp 3/4/7
/// + the methane splint). Two ways to light it (W5.8): STRIKE it — swipe the
/// held match across a striker surface (the matches box, a burner base) — or
/// hold its head near anything already hot. A lit match then ignites burners
/// (BurnerController) and fires the methane splint test. Pure predicates for
/// the suite.
public class Matchstick : MonoBehaviour
{
    public const float IgniteDistance = 0.22f;
    public const float IgniteTempC = 80f;
    public const float StrikeMinSpeed = 0.35f;
    [SerializeField] private float burnSeconds = 25f;

    private bool _lit, _spent;
    private float _litAt;
    private float _nextScan;
    private GameObject _flame;
    private XRGrab _grab;

    public bool IsLit => _lit;
    public bool IsSpent => _spent;

    /// Pure: does a heat source at this distance/temperature ignite the match?
    public static bool ShouldIgnite(float distance, float tempC, bool alreadyLit, bool spent)
        => !alreadyLit && !spent && distance <= IgniteDistance && tempC >= IgniteTempC;

    /// Pure (W5.8): does a swipe against a striker surface light the match?
    /// Must be HELD (a match knocked off the shelf doesn't self-ignite), fresh,
    /// and moving fast enough to count as a strike.
    public static bool ShouldStrike(bool held, bool lit, bool spent, float relSpeed, bool strikerSurface, float minSpeed = StrikeMinSpeed)
        => held && !lit && !spent && strikerSurface && relSpeed >= minSpeed;

    private void OnCollisionEnter(Collision collision)
    {
        if (_grab == null) _grab = GetComponent<XRGrab>();
        bool striker = collision.collider != null && collision.collider.GetComponentInParent<MatchStrikerSurface>() != null;
        if (ShouldStrike(_grab != null && _grab.isSelected, _lit, _spent, collision.relativeVelocity.magnitude, striker))
            Ignite();
    }

    private void Update()
    {
        if (_lit && Time.time - _litAt > burnSeconds) Extinguish(true);
        if (_lit || _spent || !Application.isPlaying) return;
        // Secondary path: light off an already-hot station. Throttled — the
        // scene scan used to run every frame per match.
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.5f;

        foreach (var sim in FindObjectsByType<TemperatureSim>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, sim.transform.position);
            if (ShouldIgnite(d, sim.AtLeast(IgniteTempC) ? IgniteTempC : 0f, _lit, _spent))
            {
                Ignite();
                break;
            }
        }
    }

    public void Ignite()
    {
        if (_lit || _spent) return;
        _lit = true;
        _litAt = Time.time;
        EffectVfx.FlamePop(transform.position);
        AudioService.TryPlayAt("burner-ignite", transform.position);
        if (Application.isPlaying)
        {
            _flame = new GameObject("MatchFlame");
            _flame.transform.SetParent(transform, false);
            _flame.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var light = _flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.25f);
            light.range = 0.6f;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
        }
    }

    public void Extinguish(bool spent)
    {
        _lit = false;
        if (spent) _spent = true;
        if (_flame != null) Destroy(_flame);
    }
}
