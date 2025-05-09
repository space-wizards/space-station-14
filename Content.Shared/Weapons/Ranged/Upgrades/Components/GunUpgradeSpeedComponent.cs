using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the speed of a gun's projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeSpeedComponent : Component
{
    /// <summary>
    /// Multiplier for the speed of a gun's projectile.
    /// </summary>
    [DataField]
    public float Coefficient = 1;
}
