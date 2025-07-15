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

    public static readonly Color DefaultDepartmentColor = Color.Yellow;

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

        DepartmentPrototype? chosenDepartment = null;
        foreach (var jobId in jobList)
        {
            var jobProto = protoManager.Index(jobId);

            if (!jobSystem.TryGetDepartment(jobId, out var dept))
                continue;

            if (chosenDepartment != null && chosenDepartment.Weight > dept.Weight)
                continue;

            chosenDepartment = dept;
        }

        var departmentColor = chosenDepartment?.Color ?? DefaultDepartmentColor;

        var localizedNames = jobList.Select(jobId => protoManager.Index(jobId).LocalizedName).ToList();
        var name = ContentLocalizationManager.FormatListToOr(localizedNames);

        if (trackerPrototype.Name is { } trackerName)
            name = Loc.GetString(trackerName);

        if (!Inverted)
        {
            if (roleDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-insufficient",
                ("time", formattedRoleDiff),
                ("job", name),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        if (roleDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-too-high",
                ("time", formattedRoleDiff),
                ("job", name),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        return true;
    }
}
