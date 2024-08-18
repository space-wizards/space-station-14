using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
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
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department = default!;

    /// <summary>
    /// How long (in seconds) this requirement is.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details)
    {
        details = new FormattedMessage();
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
        var nameDepartment = "role-timer-department-unknown";

        if (protoManager.TryIndex(Department, out var departmentIndexed))
        {
            nameDepartment = departmentIndexed.Name;
        }

        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-department-not-too-high" : "role-timer-department-sufficient",
            ("current", playtime.TotalMinutes),
            ("required", Time.TotalMinutes),
            ("department", Loc.GetString(nameDepartment)),
            ("departmentColor", department.Color.ToHex())));

        if (!Inverted)
        {
            if (deptDiff <= 0)
                return true;

            details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-department-insufficient",
                ("current", playtime.TotalMinutes),
                ("required", Time.TotalMinutes),
                ("department", Loc.GetString(nameDepartment)),
                ("departmentColor", department.Color.ToHex())));
            return false;
        }

        if (deptDiff <= 0)
        {
            details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-department-too-high",
                ("current", playtime.TotalMinutes),
                ("required", Time.TotalMinutes),
                ("department", Loc.GetString(nameDepartment)),
                ("departmentColor", department.Color.ToHex())));
            return false;
        }

        return true;
    }
}
