using Content.Shared.Sound;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
public sealed class NewGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundGunshot")]
    public SoundSpecifier? SoundGunshot = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/smg.ogg");

    /// <summary>
    /// Used for tracking whether we're still shooting continuously.
    /// </summary>
    [ViewVariables]
    public bool AttemptedShotLastTick;

    /// <summary>
    /// Used for tracking semi-auto / burst
    /// </summary>
    [ViewVariables]
    public int ShotCounter = 0;

    /// <summary>
    /// How many times it shoots per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fireRate")]
    public float FireRate = 2f;

    [ViewVariables, DataField("nextFire")]
    public TimeSpan NextFire = TimeSpan.Zero;
}
