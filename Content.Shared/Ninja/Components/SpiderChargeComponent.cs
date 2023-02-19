namespace Content.Shared.Ninja.Components;

[RegisterComponent]
public sealed class SpiderChargeComponent : Component
{
    /// Range for planting within the target area
    [DataField("range")]
    public float Range = 10f;

    /// The ninja that planted this charge
    [ViewVariables]
    public EntityUid? Planter = null;
}
