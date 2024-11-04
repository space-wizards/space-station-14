using System.Diagnostics.CodeAnalysis;
using Content.Shared.Localizations;
using Content.Shared._Starlight;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Player;
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

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        string proto = Role;
        //🌟Starlight🌟 start
        if (player is not null)
        {
            var roles = IoCManager.Resolve<ISharedPlayersRoleManager>().GetPlayerData(player);
            if (roles is not null && (roles.HasFlag(PlayerFlags.Staff) || roles.HasFlag(PlayerFlags.Retired) || roles.HasFlag(PlayerFlags.AlfaTester) || roles.HasFlag(PlayerFlags.Mentor)))
                return true;
        }
        //🌟Starlight🌟 end

        playTimes.TryGetValue(proto, out var roleTime);
        var roleDiffSpan = Time - roleTime;
        var roleDiff = roleDiffSpan.TotalMinutes;
        var formattedRoleDiff = ContentLocalizationManager.FormatPlaytime(roleDiffSpan);
        var departmentColor = Color.Yellow;

        if (entManager.EntitySysManager.TryGetEntitySystem(out SharedJobSystem? jobSystem))
        {
            var jobProto = jobSystem.GetJobPrototype(proto);

            if (jobSystem.TryGetDepartment(jobProto, out var departmentProto))
                departmentColor = departmentProto.Color;
        }

        if (!Inverted)
        {
            if (roleDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-insufficient",
                ("time", formattedRoleDiff),
                ("job", Loc.GetString(proto)),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        if (roleDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-role-too-high",
                ("time", formattedRoleDiff),
                ("job", Loc.GetString(proto)),
                ("departmentColor", departmentColor.ToHex())));
            return false;
        }

        return true;
    }
}
