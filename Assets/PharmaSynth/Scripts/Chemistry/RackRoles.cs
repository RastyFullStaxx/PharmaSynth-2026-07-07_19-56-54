using System.Collections.Generic;

/// Who has claimed which role inside ONE rack group (W5.52).
///
/// A rack group's tubes are interchangeable, so a role belongs to whichever tube the
/// player actually poured it into. This is the shared ledger that keeps that exclusive:
/// without it two tubes both narrow to the same role, the RackTaskGroup counts that role
/// twice, and the step completes while a tube still sits empty.
///
/// Plain C# rather than a MonoBehaviour: it holds no transform, needs no update, and is
/// created by ExperimentSceneBuilder alongside the group's RackTaskGroups.
public class RackRoles
{
    private readonly Dictionary<LiquidTaskBinding, int> _claims
        = new Dictionary<LiquidTaskBinding, int>();

    public void Claim(LiquidTaskBinding who, int role)
    {
        if (who == null) return;
        _claims[who] = role;
    }

    /// Roles claimed by every OTHER member — what this tube may no longer become.
    /// Excluding the caller matters: a tube must not be blocked by its own claim, or it
    /// would lose the role it just took and start oscillating.
    public ICollection<int> TakenByOthers(LiquidTaskBinding me)
    {
        var taken = new HashSet<int>();
        foreach (var kv in _claims)
            if (kv.Key != null && kv.Key != me) taken.Add(kv.Value);
        return taken;
    }

    // No Clear(): ExperimentSceneBuilder creates a NEW ledger every time it wires a rack,
    // so a retry starts with no claims by construction. A reset method would only be a
    // second way to do what rebuilding already does.
}
