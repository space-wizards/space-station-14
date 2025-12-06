using Robust.Shared.Prototypes;

namespace Content.Shared.Players.PlayTimeTracking;

/// <summary>
/// Given to a role to specify its ID for role-timer tracking purposes. That's it.
/// </summary>
[Prototype]
public sealed partial class PlayTimeTrackerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localized name ID of this playtime tracker.
    /// If this field is left null, it'll default to a list of every job that has this tracker.
    /// </summary>
    [DataField]
    public LocId? Name { get; private set; } = default!;

    /// <summary>
    /// Whether this tracker should appear in the playtime stats menu.
    /// </summary>
    [DataField]
    public bool ShowInStatsMenu { get; private set; } = true;
}
