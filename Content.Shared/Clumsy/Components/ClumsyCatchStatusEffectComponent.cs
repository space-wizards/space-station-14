using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to catch items thrown at it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyCatchStatusEffectComponent : BaseClumsyStatusEffectComponent
{
    /// <summary>
    /// How much damage to take after failing.
    /// </summary>
    [DataField]
    public DamageSpecifier? FailDamage;
}
