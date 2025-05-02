namespace Content.Shared.Movement.Events;

/// <summary>
///     This event can completely bulldoze friction values calculated by the SharedMoverController.
///     You should use this event unless you know exactly what you're doing and the potential consequences of your actions.
///     This should only be used in scenarios where you are absolutely certain that you don't care about friction
///     modifiers, and you can prioritize and account for multiple bulldozing attempts.
///     This Event can and will break prediction if you don't know what you're doing.
/// </summary>
[ByRefEvent]
public record struct MobFrictionBulldozeEvent
{
    public float Friction;
    public float Acceleration;
    public float MinFrictionSpeed;
}
