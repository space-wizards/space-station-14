namespace Content.Shared.Friction;

/// <summary>
/// An entity with this component will have their tileFriction overwritten with these value
/// </summary>
[RegisterComponent]
public sealed partial class TileFrictionOverwrittenComponent : Component
{
    [DataField]
    public float? Friction;

    [DataField]
    public float? MobFriction;

    [DataField]
    public float? MobAcceleration;
}
