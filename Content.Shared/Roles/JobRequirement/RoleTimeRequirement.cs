using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Localizations;
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
    public ProtoId<PlayTimeTrackerPrototype> Role;

    /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
    [DataField(required: true)]
    public TimeSpan Time;

    private static readonly Color DefaultDepartmentColor = Color.Yellow;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        var trackerPrototype = protoManager.Index(Role);
        var jobSystem = entManager.EntitySysManager.GetEntitySystem<SharedJobSystem>();

        playTimes.TryGetValue(Role, out var roleTime);
        var roleDiffSpan = Time - roleTime;
        var roleDiff = roleDiffSpan.TotalMinutes;
        var formattedRoleDiff = ContentLocalizationManager.FormatPlaytime(roleDiffSpan);

        var jobList = jobSystem.GetJobPrototypes(Role);

        var departmentColor = DefaultDepartmentColor;

        if (jobSystem.TryGetListHighestWeightDepartment(jobList, out var department))
            departmentColor = department.Color;

        var localizedNames = jobList.Select(jobId => protoManager.Index(jobId).LocalizedName).ToList();
        var names = ContentLocalizationManager.FormatListToOr(localizedNames);

        if (trackerPrototype.Name is { } trackerName)
            names = Loc.GetString(trackerName);

        if (!Inverted)
        {
            if (roleDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-insufficient",
                ("time", formattedRoleDiff),
                ("job", names),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        if (roleDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-too-high",
                ("time", formattedRoleDiff),
                ("job", names),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        return true;
    }
}
