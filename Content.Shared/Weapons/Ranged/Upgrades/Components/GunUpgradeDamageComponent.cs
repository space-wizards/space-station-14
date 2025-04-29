using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the damage of a gun's projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeDamageComponent : Component
{
    /// <summary>
    /// Additional damage added onto the projectile's base damage.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();
}
