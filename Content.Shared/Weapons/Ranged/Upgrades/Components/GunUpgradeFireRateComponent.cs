using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the firerate of a gun.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeFireRateComponent : Component
{
    /// <summary>
    /// Multiplier for the speed of a gun's fire rate.
    /// </summary>
    [DataField]
    public float Coefficient = 1;
}
