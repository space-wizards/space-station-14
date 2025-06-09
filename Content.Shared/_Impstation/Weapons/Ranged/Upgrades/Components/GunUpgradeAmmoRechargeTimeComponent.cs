using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GunUpgradeAmmoRechargeTimeComponent : Component
{
    /// <summary>
    /// Multiplier for the speed of a gun's ammo regen. lower is better.
    /// </summary>
    [DataField]
    public float Coefficient = 1;
}
