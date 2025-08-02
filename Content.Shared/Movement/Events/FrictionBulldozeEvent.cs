namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised at the end of HandleMobMovement in case we want to bulldoze the calculated values
/// Raised at the end because we don't know what we are bulldozing
/// WARNING: DO NOT USE THIS EVENT UNLESS YOU KNOW EXACTLY WHAT YOU ARE DOING!!!
/// THIS EVENT SHOULD ONLY BE USED AS A LAST RESORT IF THERE ARE NO OTHER OPTIONS INCLUDING ADDING NEW EVENTS!!!
/// </summary>
[ByRefEvent]
public record struct FrictionBulldozeEvent
{
    public float Friction;
    public float MinimumFrictionSpeed;
    public float Acceleration;
}
