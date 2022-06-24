namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity to check if it can move while weightless.
/// </summary>
[ByRefEvent]
public struct CanWeightlessMoveEvent
{
    public bool CanMove = false;

    public CanWeightlessMoveEvent()
    {
    }
}
