using System.Diagnostics.CodeAnalysis;
using Content.Shared.Localizations;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

/// <summary>
/// Calculates antag group playtime requirements. Mostly copies DepartmentTimeRequirement.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AntagGroupTimeRequirement : JobRequirement
{
    /// <summary>
    /// Which antag group needs the required amount of time.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AntagGroupPrototype> AntagGroup;

    /// <summary>
    /// How long (in seconds) this requirement is.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        var playtime = TimeSpan.Zero;

        // Check all antags' antag groups.
        var group = protoManager.Index(AntagGroup);
        var antags = group.Roles;
        string proto;

        // Check all antags' playtime
        foreach (var other in antags)
        {
            // The schema is stored on the Antag role but we want to explode if the timer isn't found anyway.
            proto = protoManager.Index(other).PlayTimeTracker;

            playTimes.TryGetValue(proto, out var otherTime);
            playtime += otherTime;
        }

        var groupDiffSpan = Time - playtime;
        var groupDiff = groupDiffSpan.TotalMinutes;
        var formattedGroupDiff = ContentLocalizationManager.FormatPlaytime(groupDiffSpan);
        var nameGroup = "role-timer-antag-group-unknown";

        if (protoManager.TryIndex(AntagGroup, out var groupIndexed))
        {
            nameGroup = groupIndexed.Name;
        }

        if (!Inverted)
        {
            if (groupDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-group-insufficient",
                ("time", formattedGroupDiff),
                ("group", Loc.GetString(nameGroup)),
                ("groupColor", group.Color.ToHex())));
            return false;
        }

        if (groupDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-group-insufficient",
                ("time", formattedGroupDiff),
                ("group", Loc.GetString(nameGroup)),
                ("groupColor", group.Color.ToHex())));
            return false;
        }

        return true;
    }
}
