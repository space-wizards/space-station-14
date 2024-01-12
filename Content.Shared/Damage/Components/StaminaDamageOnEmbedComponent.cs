using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when embeds in an entity.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(StaminaSystem))]
public sealed partial class StaminaDamageOnEmbedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 10f;
}
