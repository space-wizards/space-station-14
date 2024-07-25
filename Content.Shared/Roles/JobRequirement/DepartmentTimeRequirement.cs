using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class DepartmentTimeRequirement : JobRequirement
{
    /// <summary>
    /// Which department needs the required amount of time.
    /// </summary>
    [DataField]
    public ProtoId<DepartmentPrototype> Department = default!;

    /// <summary>
    /// How long (in seconds) this requirement is.
    /// </summary>
    [DataField]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        var playtime = TimeSpan.Zero;

        // Check all jobs' departments
        var department = protoManager.Index(Department);
        var jobs = department.Roles;
        string proto;

        // Check all jobs' playtime
        foreach (var other in jobs)
        {
            // The schema is stored on the Job role but we want to explode if the timer isn't found anyway.
            proto = protoManager.Index(other).PlayTimeTracker;

            playTimes.TryGetValue(proto, out var otherTime);
            playtime += otherTime;
        }

        var deptDiff = Time.TotalMinutes - playtime.TotalMinutes;

        if (!Inverted)
        {
            if (deptDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-department-insufficient",
                ("time", Math.Ceiling(deptDiff)),
                ("department", Loc.GetString(Department)),
                ("departmentColor", department.Color.ToHex())));
            return false;
        }

        if (deptDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-department-too-high",
                ("time", -deptDiff),
                ("department", Loc.GetString(Department)),
                ("departmentColor", department.Color.ToHex())));
            return false;
        }

        return true;
    }
}
