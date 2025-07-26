namespace Content.Shared.Friction;

/// <summary>
/// This is used to let an entity bulldoze tileFriction for colliding entities with a specified value
/// </summary>
[RegisterComponent]
public sealed partial class TileFrictionBulldozerComponent : Component
{

    /// <summary>
    /// These three datafields correspond to the three friction datafields tiles have.
    /// If defined they will overwrite the corresponding friction value normally given by a tile.
    /// </summary>
    [DataField]
    public float? Friction;

    [DataField]
    public float? MobFriction;

    [DataField]
    public float? MobAcceleration;
}
