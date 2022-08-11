namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// The rotating part of a gimbal gun.
/// </summary>
[RegisterComponent]
public sealed class GimbalGunWeaponComponent : Component
{
    /// <summary>
    /// The stationary base piece of this gun.
    /// </summary>
    public GimbalGunBaseComponent? Base = null;

    /// <summary>
    /// The entity the gimbal gun will actively try to turn towards at all times.
    /// </summary>
    public GimbalGunTargetComponent? Target = null;
}
