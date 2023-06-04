using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for adding in bonus damage via <see cref="GetMeleeWeaponEvent"/>
/// This exists only for event relays and doing entity shenanigans.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class BonusMeleeDamageComponent : Component
{
    /// <summary>
    /// The damage that will be applied.
    /// </summary>
    [DataField("bonusDamage", required: true)]
    public DamageSpecifier BonusDamage = default!;
}
