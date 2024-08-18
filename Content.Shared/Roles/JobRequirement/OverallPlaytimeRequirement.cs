using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class OverallPlaytimeRequirement : JobRequirement
{
    /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details)
    {
        var overallTime = playTimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
        var overallDiff = Time.TotalMinutes - overallTime.TotalMinutes;

        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-overall-not-too-high" : "role-timer-overall-sufficient",
            ("current", overallTime.TotalMinutes),
            ("required", Time.TotalMinutes)));

        if (!Inverted)
        {
            if (overallDiff <= 0 || overallTime >= Time)
                return true;

            details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-overall-insufficient",
                ("current", overallTime.TotalMinutes),
                ("required", Time.TotalMinutes)));
            return false;
        }

        if (overallDiff <= 0 || overallTime >= Time)
        {
            details = FormattedMessage.FromMarkupPermissive(
                Loc.GetString("role-timer-overall-too-high",
                ("current", overallTime.TotalMinutes),
                ("required", Time.TotalMinutes)));
            return false;
        }

        return true;
    }
}
