using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Marks the glove entity as finger guns. Holds the paired gun hidden in <see cref="ContainerId"/> until gun is swapped to
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsComponent : Component
{
    /// <summary>
    /// The gun prototype spawned inside <see cref="ContainerId"/> the first time the gloves are initialized
    /// </summary>
    [DataField]
    public EntProtoId GunPrototype = "WeaponFingerGunsGun";

    /// <summary>
    /// The container the paired gun is stashed in while the gloves are active
    /// </summary>
    [DataField]
    public string ContainerId = "finger_gun";
}

/// <summary>
/// Marks the hidden gun entity as the finger guns weapon. Its paired glove is stashed in
/// <see cref="ContainerId"/> while the gun is active
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsGunComponent : Component
{
    /// <summary>
    /// The container the paired glove is stashed in while the gun is active
    /// </summary>
    [DataField]
    public string ContainerId = "finger_glove";
}
