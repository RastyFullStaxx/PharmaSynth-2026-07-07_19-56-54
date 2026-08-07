/// Tutorial Mode (2026-08-07): all 9 experiments unlocked, heavily guided
/// (glow + waypoint + hint on the watch + always-on labels), and UNGRADED —
/// no quiz, no grade screen, no BKT update, no save write, no unlock.
/// Practice only; a run leaves no trace.
///
/// Deliberately the same shape as DemoSession.Active: one static flag every
/// consumer early-returns on, so the campaign path stays unchanged by
/// construction rather than by testing.
public static class TutorialSession
{
    public static bool Active;
}
