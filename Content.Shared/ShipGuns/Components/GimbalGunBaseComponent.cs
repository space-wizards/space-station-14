namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// The base, stationary part of a gimbal gun.
/// </summary>
[RegisterComponent]
public sealed class GimbalGunBaseComponent : Component
{
    /// <summary>
    /// The gimbal weapon's base.
    /// </summary>
    public GimbalGunWeaponComponent? Weapon;
}
