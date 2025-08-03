using System.Diagnostics.CodeAnalysis;
using Content.Shared.Localizations;
using Content.Shared.Starlight;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared._NullLink;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class OverallPlaytimeRequirement : JobRequirement
{
    /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan>? playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        // If playTimes is null, we're not going to check against playtime requirements
        if (playTimes == null)
            return true;

        //NullLink start
        if (player is not null && IoCManager.Resolve<ISharedNullLinkPlayerRolesReqManager>().IsAllRolesAvailable(player))
            return true;
        //NullLink end

        var overallTime = playTimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
        var overallDiffSpan = Time - overallTime;
        var overallDiff = overallDiffSpan.TotalMinutes;
        var formattedOverallDiff = ContentLocalizationManager.FormatPlaytime(overallDiffSpan);

        if (!Inverted)
        {
            if (overallDiff <= 0 || overallTime >= Time)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-overall-insufficient",
                ("time", formattedOverallDiff)));
            return false;
        }

        if (overallDiff <= 0 || overallTime >= Time)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-overall-too-high",
                ("time", formattedOverallDiff)));
            return false;
        }

        return true;
    }
}
