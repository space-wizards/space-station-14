using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for adding in bonus damage via <see cref="GetMeleeWeaponEvent"/>
/// This exists only for event relays and doing entity shenanigans.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class BonusMeleeDamageComponent : Component
{
    /// <summary>
    /// The damage that will be added.
    /// </summary>
    [DataField("bonusDamage")]
    public DamageSpecifier? BonusDamage;

    /// <summary>
    /// A modifier set for the damage that will be dealt.
    /// </summary>
    [DataField("damageModifierSet")]
    public DamageModifierSet? DamageModifierSet;

    /// <summary>
    /// A flat damage increase added to <see cref="GetHeavyDamageModifierEvent"/>
    /// </summary>
    [DataField("heavyDamageFlatModifier"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HeavyDamageFlatModifier;

    /// <summary>
    /// A value multiplier by the value of <see cref="GetHeavyDamageModifierEvent"/>
    /// </summary>
    [DataField("heavyDamageMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float HeavyDamageMultiplier = 1;
}
