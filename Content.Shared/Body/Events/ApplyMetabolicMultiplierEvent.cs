namespace Content.Shared.Body.Events;

// TODO REFACTOR THIS
// This will cause rates to slowly drift over time due to floating point errors.
// Instead, the system that raised this should trigger an update and subscribe to get-modifier events.
[ByRefEvent]
public readonly record struct ApplyMetabolicMultiplierEvent(
    EntityUid Uid,
    float Multiplier,
    bool Apply)
{
    /// <summary>
    /// The entity whose metabolism is being modified.
    /// </summary>
    public readonly EntityUid Uid = Uid;

    /// <summary>
    /// What the metabolism's update rate will be multiplied by.
    /// </summary>
    public readonly float Multiplier = Multiplier;

    /// <summary>
    /// If true, apply the multiplier. If false, revert it.
    /// </summary>
    public readonly bool Apply = Apply;
}
