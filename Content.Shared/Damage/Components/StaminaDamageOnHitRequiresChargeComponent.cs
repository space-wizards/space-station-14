using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// With this component, <see cref="StaminaDamageOnHitComponent"/> will consume battery charge and will not work if there is less than needed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStaminaSystem))]
public sealed partial class StaminaDamageOnHitRequiresChargeComponent : Component
{
    /// <summary>
    /// The amount of battery charge required for the hit.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float RequiredCharge;
}
