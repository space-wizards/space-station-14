namespace Content.Shared.Movement;

/// <summary>
/// Contains all of the relevant data for mob movement.
/// Raised on a mob if something wants to overwrite its movement characteristics.
/// </summary>
[ByRefEvent]
public struct MobMovementProfileEvent
{
    /// <summary>
    /// Should we use this profile instead of the entity's default?
    /// </summary>
    public bool Override = false;

    public readonly bool Touching;
    public readonly bool Weightless;

    public float Friction;
    public float WeightlessModifier;
    public float Acceleration;

    public MobMovementProfileEvent(
        bool touching,
        bool weightless,
        float friction,
        float weightlessModifier,
        float acceleration)
    {
        Touching = touching;
        Weightless = weightless;
        Friction = friction;
        WeightlessModifier = weightlessModifier;
        Acceleration = acceleration;
    }
}
