using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
///     Applies the specified DamageModifierSets when the entity takes damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageProtectionBuffComponent : Component
{
    /// <summary>
    ///     The damage modifiers for entities with this component.
    /// </summary>
    [DataField]
    public Dictionary<string, DamageModifierSetPrototype> Modifiers = new();
}
