using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to fire a gun.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyGunStatusEffectComponent : BaseClumsyStatusEffectComponent
{
    /// <summary>
    /// How long to be stunned after failing.
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// How much damage to take after failing.
    /// </summary>
    [DataField]
    public DamageSpecifier? FailDamage;
}
