namespace Content.Shared.Friction;

/// <summary>
/// This is used to let an entity bulldoze tileFriction for colliding entities with a specified value
/// </summary>
[RegisterComponent]
public sealed partial class TileFrictionBulldozerComponent : Component
{
    [DataField]
    public float? Friction;

    [DataField]
    public float? MobFriction;

    [DataField]
    public float? MobAcceleration;
}
