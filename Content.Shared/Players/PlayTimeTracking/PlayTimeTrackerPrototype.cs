using Robust.Shared.Prototypes;

namespace Content.Shared.Players.PlayTimeTracking;

/// <summary>
/// Given to a role to specify its ID for role-timer tracking purposes. That's it.
/// </summary>
[Prototype("playTimeTracker")]
public readonly record struct PlayTimeTrackerPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
}
