using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when embeds in an entity.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(StaminaSystem))]
public sealed partial class StaminaDamageOnEmbedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Damage = 10f;
}
