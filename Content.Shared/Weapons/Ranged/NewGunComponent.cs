using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
public sealed class NewGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundGunshot")]
    public SoundSpecifier? SoundGunshot = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/smg.ogg");

    /// <summary>
    /// Where the gun is being requested to shoot.
    /// </summary>
    [ViewVariables]
    public MapCoordinates? ShootCoordinates = null;

    /// <summary>
    /// Used for tracking semi-auto / burst
    /// </summary>
    [ViewVariables]
    public int ShotCounter = 0;

    /// <summary>
    /// How many times it shoots per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fireRate")]
    public float FireRate = 6f;

    /// <summary>
    /// When the gun is next available to be shot.
    /// Can be set multiple times in a single tick due to guns firing faster than a single tick time.
    /// </summary>
    [ViewVariables, DataField("nextFire")]
    public TimeSpan NextFire = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("selectiveFire")]
    public SelectiveFire SelectiveFire = SelectiveFire.FullAuto;
}

public enum SelectiveFire : byte
{
    Safety = 0,
    SemiAuto = 1 << 0,
    Burst = 1 << 2,
    FullAuto = 1 << 3, // Not in the building!
}
