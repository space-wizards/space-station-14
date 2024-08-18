using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class RoleTimeRequirement : JobRequirement
{
    /// <summary>
    /// What particular role they need the time requirement with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PlayTimeTrackerPrototype> Role = default!;

    /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details)
    {
        details = new FormattedMessage();

        string proto = Role;

        playTimes.TryGetValue(proto, out var roleTime);
        var roleDiff = Time.TotalMinutes - roleTime.TotalMinutes;
        var departmentColor = Color.Yellow;

        if (entManager.EntitySysManager.TryGetEntitySystem(out SharedJobSystem? jobSystem))
        {
            var jobProto = jobSystem.GetJobPrototype(proto);

            if (jobSystem.TryGetDepartment(jobProto, out var departmentProto))
                departmentColor = departmentProto.Color;
        }

        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-not-too-high" : "role-timer-role-sufficient",
            ("current", roleTime.TotalMinutes),
            ("required", Time.TotalMinutes),
            ("job", Loc.GetString(proto)),
            ("departmentColor", departmentColor.ToHex())));

        if (!Inverted)
        {
            if (roleDiff <= 0)
                return true;

            details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-insufficient",
                ("current", roleTime.TotalMinutes),
                ("required", Time.TotalMinutes),
                ("job", Loc.GetString(proto)),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        if (roleDiff <= 0)
        {
            details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-too-high",
                ("current", roleTime.TotalMinutes),
                ("required", Time.TotalMinutes),
                ("job", Loc.GetString(proto)),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        return true;
    }
}
