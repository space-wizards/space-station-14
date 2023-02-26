namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for the Space Ninja's unique Spider Charge.
/// Only this component detonating can trigger the ninja's objective.
/// </summary>
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
