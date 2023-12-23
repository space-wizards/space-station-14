using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for the Space Ninja's unique Spider Charge.
/// Only this component detonating can trigger the ninja's objective.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpiderChargeComponent : Component
{
    /// Range for planting within the target area
    [DataField("range")]
    public float Range = 10f;

    /// The ninja that planted this charge
    [DataField("planter")]
    public EntityUid? Planter = null;
}
