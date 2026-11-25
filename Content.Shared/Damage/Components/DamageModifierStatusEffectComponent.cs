using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Component used on a status effect entity to modify damage taken.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageModifierStatusEffectComponent : Component
{
    /// <summary>
    /// The modifier prototype to apply.
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers;
}
