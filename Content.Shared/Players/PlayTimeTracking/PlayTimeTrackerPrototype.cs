using Content.Shared.Roles;
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
    /// Only useful for jobs that share playtime trackers.
    /// </summary>
    [DataField]
    public LocId? Name { get; private set; } = default!;

    /// <summary>
    /// The ID of the department this job is apart of, if any.
    /// Only useful for jobs that share playtime trackers.
    /// </summary>
    [DataField]
    public ProtoId<DepartmentPrototype>? Department { get; private set; } = default!;
}
