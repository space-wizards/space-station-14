using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
public sealed class NewGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundGunshot")]
    public SoundSpecifier? SoundGunshot = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/smg.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundEmpty")]
    public SoundSpecifier? SoundEmpty = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    /// <summary>
    /// Sound played when toggling the <see cref="SelectedMode"/> for this gun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundSelective")]
    public SoundSpecifier? SoundSelectiveToggle;

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

    /// <summary>
    /// What firemodes can be selected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("availableModes")]
    public SelectiveFire AvailableModes = SelectiveFire.Safety | SelectiveFire.FullAuto;

    /// <summary>
    /// What firemode is currently selected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("selectedMode")]
    public SelectiveFire SelectedMode = SelectiveFire.FullAuto;

    [DataField("selectModeAction")]
    public InstantAction? SelectModeAction;

    /// <summary>
    /// Used for sloth's debugging. Will be removed on undraft.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int FakeAmmo = 30;
}

[Flags]
public enum SelectiveFire : byte
{
    Invalid = 0,
    Safety = 1 << 0,
    SemiAuto = 1 << 1,
    Burst = 1 << 2,
    FullAuto = 1 << 3, // Not in the building!
}
