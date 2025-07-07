using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for adding piercing to a gun.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradePenetrationComponent : Component
{
    /// <summary>
    /// The penetration threshold
    /// </summary>
    [DataField]
    public float Threshold = 0;
}
