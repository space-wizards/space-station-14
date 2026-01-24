using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Enables a gun to use an "alt fire" that shoots the gun in burst fire mode.
/// Requires <see cref="GunComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunAltBurstComponent : Component
{
    /// <summary>
    /// If true, the gun will attempt to fire the entire burst even if the ammo is gone.
    /// </summary>
    [DataField]
    public bool ForceEntireBurst = true;
}
