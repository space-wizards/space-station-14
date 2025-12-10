using Content.Shared.Damage.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Component used for stun batons.
/// Works in combintation with <see cref="StaminaDamageOnHitComponent"/>, <see cref="PredictedBatteryComponent"/> and <see cref="ItemToggleComponent"/>
/// to make the entity require battery charge to deal stamina damage to someone while it is toggled on and used as a weapon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StunbatonSystem))]
public sealed partial class StunbatonComponent : Component
{
    /// <summary>
    /// The charge required per hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyPerUse = 350;
}
