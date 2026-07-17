using UnityEngine;

/// Marker: a pour STREAM passes straight through this object (funnels). Without
/// it, LiquidPourer.ResolveTarget treated the funnel's collider as the landing
/// surface — no LiquidPhysics there, so the hydrolysate the manuscript says to
/// filter was WASTED as a puddle on top of the funnel and the beaker below
/// stayed empty, leaving the FeCl3 filtrate test nothing to react with
/// (found by the 2026-07-17 player-path simulation). The ray continues to the
/// receiving vessel underneath, which is what a funnel is for.
public class LiquidPassthrough : MonoBehaviour { }
