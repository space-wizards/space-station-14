namespace Content.Shared.Movement.Events;

/// <summary>
/// This is used for overriding tileDef Friction values
/// Because this is explicitly for bulldozing only subscribe to this if you know what you're doing
/// </summary>
[ByRefEvent]
public record struct MoverTileDefEvent
{
    public float? Friction;

    public float? MobFriction;

    public float? MobAcceleration;
}
