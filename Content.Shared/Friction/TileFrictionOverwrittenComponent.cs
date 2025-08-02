namespace Content.Shared.Friction;

/// <summary>
/// An entity with this component will have their tileFriction overwritten with these values
/// </summary>
[RegisterComponent]
public sealed partial class TileFrictionOverwrittenComponent : Component
{
    /// <summary>
    /// These three datafields correspond to the three friction datafields tiles have.
    /// If defined they will take precedent over the values given by tileDef
    /// </summary>
    [DataField]
    public float? Friction;

    [DataField]
    public float? MobFriction;

    [DataField]
    public float? MobAcceleration;
}
