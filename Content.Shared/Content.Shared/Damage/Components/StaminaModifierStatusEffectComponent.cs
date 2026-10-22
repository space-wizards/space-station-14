using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Multiplies the entity's <see cref="StaminaComponent.StaminaDamage"/> by the <see cref="Modifier"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStaminaSystem))]
public sealed partial class StaminaModifierStatusEffectComponent : Component
{
    /// <summary>
    /// What to multiply max stamina by.
    /// When added this scales max stamina, but not stamina damags to give you an extra boost of survability.
    /// If you have too much damage when the modifier is removed, you suffer "withdrawl" and instantly stamcrit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("modifier"), AutoNetworkedField]
    public float Modifier = 2f;
}
