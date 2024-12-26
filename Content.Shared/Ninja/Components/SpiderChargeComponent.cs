using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for the Space Ninja's unique Spider Charge.
/// Only this component detonating can trigger the ninja's objective.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpiderChargeSystem))]
public sealed partial class SpiderChargeComponent : Component
{
    /// Range for planting within the target area
    [DataField]
    public float Range = 10f;

    /// The ninja that planted this charge
    [DataField]
    public EntityUid? Planter;
}
