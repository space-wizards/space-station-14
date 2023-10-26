using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, Virtual]
[AutoGenerateComponentState]
public partial class GunComponent : Component
{
    #region Sound

    [ViewVariables(VVAccess.ReadWrite), DataField("soundGunshot")]
    public SoundSpecifier? SoundGunshot = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/smg.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundEmpty")]
    public SoundSpecifier? SoundEmpty = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    /// <summary>
    /// Sound played when toggling the <see cref="SelectedMode"/> for this gun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundMode")]
    public SoundSpecifier? SoundModeToggle = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/selector.ogg");

    #endregion

    #region Recoil

    // These values are very small for now until we get a debug overlay and fine tune it

    /// <summary>
    /// A scalar value applied to the vector governing camera recoil.
    /// If 0, there will be no camera recoil.
    /// </summary>
    [DataField("cameraRecoilScalar"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CameraRecoilScalar = 1f;

    /// <summary>
    /// Last time the gun fired.
    /// Used for recoil purposes.
    /// </summary>
    [DataField("lastFire")]
    public TimeSpan LastFire = TimeSpan.Zero;

    /// <summary>
    /// What the current spread is for shooting. This gets changed every time the gun fires.
    /// </summary>
    [DataField("currentAngle")]
    [AutoNetworkedField]
    public Angle CurrentAngle;

    /// <summary>
    /// How much the spread increases every time the gun fires.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("angleIncrease")]
    public Angle AngleIncrease = Angle.FromDegrees(0.5);

    /// <summary>
    /// How much the <see cref="CurrentAngle"/> decreases per second.
    /// </summary>
    [DataField("angleDecay")]
    public Angle AngleDecay = Angle.FromDegrees(4);

    /// <summary>
    /// The maximum angle allowed for <see cref="CurrentAngle"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxAngle")]
    [AutoNetworkedField]
    public Angle MaxAngle = Angle.FromDegrees(2);

    /// <summary>
    /// The minimum angle allowed for <see cref="CurrentAngle"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("minAngle")]
    [AutoNetworkedField]
    public Angle MinAngle = Angle.FromDegrees(1);

    #endregion

    /// <summary>
    /// Whether this gun is shot via the use key or the alt-use key.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("useKey"), AutoNetworkedField]
    public bool UseKey = true;

    /// <summary>
    /// Where the gun is being requested to shoot.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? ShootCoordinates = null;

    /// <summary>
    /// Used for tracking semi-auto / burst
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public int ShotCounter = 0;

    /// <summary>
    /// How many times it shoots per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fireRate")]
    [AutoNetworkedField]
    public float FireRate = 8f;

    /// <summary>
    /// Starts fire cooldown when equipped if true.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("resetOnHandSelected")]
    public bool ResetOnHandSelected = true;

    /// <summary>
    /// How fast the projectile moves.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("projectileSpeed")]
    public float ProjectileSpeed = 25f;

    /// <summary>
    /// When the gun is next available to be shot.
    /// Can be set multiple times in a single tick due to guns firing faster than a single tick time.
    /// </summary>
    [DataField("nextFire", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan NextFire = TimeSpan.Zero;

    /// <summary>
    /// What firemodes can be selected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("availableModes")]
    [AutoNetworkedField]
    public SelectiveFire AvailableModes = SelectiveFire.SemiAuto;

    /// <summary>
    /// What firemode is currently selected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("selectedMode")]
    [AutoNetworkedField]
    public SelectiveFire SelectedMode = SelectiveFire.SemiAuto;

    /// <summary>
    /// Whether or not information about
    /// the gun will be shown on examine.
    /// </summary>
    [DataField("showExamineText")]
    public bool ShowExamineText = true;

    /// <summary>
    /// Whether or not someone with the
    /// clumsy trait can shoot this
    /// </summary>
    [DataField("clumsyProof")]
    public bool ClumsyProof = false;
}

[Flags]
public enum SelectiveFire : byte
{
    Invalid = 0,
    // Combat mode already functions as the equivalent of Safety
    SemiAuto = 1 << 0,
    Burst = 1 << 1,
    FullAuto = 1 << 2, // Not in the building!
}
