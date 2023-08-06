namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity to check if it can move while the BodyStatus is InAir.
/// </summary>
[ByRefEvent]
public record struct CanInAirMoveEvent(EntityUid Uid)
{
    public bool CanMove = false;
}
