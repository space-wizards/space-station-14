using Content.Shared.Containers.ItemSlots;
using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nuke;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedNukeSystem))]
public sealed partial class NukeComponent : Component
{
    public const string NukeDiskSlotId = "Nuke";

    /// <summary>
    /// Cooldown time between attempts to enter the nuke code.
    /// Used to prevent clients from trying to brute force it.
    /// </summary>
    public static readonly TimeSpan EnterCodeCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Default bomb timer.
    /// </summary>
    [DataField]
    public TimeSpan Timer = TimeSpan.FromSeconds(300);

    /// <summary>
    /// If the nuke is disarmed, this sets the minimum amount of time the timer can have.
    /// The remaining time will reset to this value if it is below it.
    /// </summary>
    [DataField]
    public TimeSpan MinimumTime = TimeSpan.FromSeconds(180);

    /// <summary>
    /// The actual timer used when the nuke is armed.
    /// Not a live timer, see <see cref="ExplosionTime"/>.
    /// </summary>
    [DataField]
    public TimeSpan ArmingTime;

    /// <summary>
    /// How long until the bomb can arm again after deactivation.
    /// Used to prevent announcements spam.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The <see cref="ItemSlot"/> that stores the nuclear disk. The entity whitelist, sounds, and some other
    /// behaviours are specified by this <see cref="ItemSlot"/> definition. Make sure the whitelist, is correct
    /// otherwise a blank bit of paper will work as a "disk".
    /// </summary>
    [DataField]
    public ItemSlot DiskSlot = new();

    /// <summary>
    /// When this time is left, nuke will play last alert sound
    /// </summary>
    [DataField]
    public TimeSpan AlertSoundTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a user must wait to disarm the bomb.
    /// </summary>
    [DataField]
    public float DisarmDoAfterLength = 30.0f;

    [DataField] public string AlertLevelOnActivate = default!;
    [DataField] public string AlertLevelOnDeactivate = default!;

    /// <summary>
    /// This is stored so we can do a funny by making 0 shift the last played note up by 12 semitones (octave)
    /// </summary>
    public int LastPlayedKeypadSemitones = 0;

    [DataField]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    [DataField]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Machines/Nuke/nuke_alarm.ogg");

    [DataField]
    public SoundSpecifier ArmSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    [DataField]
    public SoundSpecifier DisarmSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    [DataField]
    public SoundSpecifier ArmMusic = new SoundCollectionSpecifier("NukeMusic");

    // These datafields here are duplicates of those in explosive component. But I'm hesitant to use explosive
    // component, just in case at some point, somehow, when grenade crafting added in someone manages to wire up a
    // proximity trigger or something to the nuke and set it off prematurely. I want to make sure they MEAN to set of
    // the nuke.
    #region ExplosiveComponent
    /// <summary>
    /// The explosion prototype. This determines the damage types, the tile-break chance, and some visual
    /// information (e.g., the light that the explosion gives off).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ExplosionPrototype> ExplosionType;

    /// <summary>
    /// The maximum intensity the explosion can have on a single time. This limits the maximum damage and tile
    /// break chance the explosion can achieve at any given location.
    /// </summary>
    [DataField]
    public float MaxIntensity = 100;

    /// <summary>
    /// How quickly the intensity drops off as you move away from the epicenter.
    /// </summary>
    [DataField]
    public float IntensitySlope = 5;

    /// <summary>
    /// The total intensity of this explosion. The radius of the explosion scales like the cube root of this
    /// number (see <see cref="ExplosionSystem.RadiusToIntensity"/>).
    /// </summary>
    [DataField]
    public float TotalIntensity = 100000;

    /// <summary>
    /// Avoid somehow double-triggering this explosion.
    /// </summary>
    public bool Exploded;
    #endregion

    /// <summary>
    /// Origin station of this bomb, if it exists.
    /// If this doesn't exist, then the origin grid and map will be filled in, instead.
    /// </summary>
    public EntityUid? OriginStation;

    /// <summary>
    /// Origin map and grid of this bomb.
    /// If a station wasn't tied to a given grid when the bomb was spawned, this will be filled in instead.
    /// </summary>
    public (MapId, EntityUid?)? OriginMapGrid;

    [DataField] public int CodeLength = 6;
    [DataField(serverOnly: true)] public string Code = string.Empty;

    /// <summary>
    /// Game time at which the nuke explodes.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? ExplosionTime;

    /// <summary>
    /// Game time at which the nuke can be rearmed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? CooldownTime;

    /// <summary>
    /// Current nuclear code buffer. Entered manually by players.
    /// If valid it will allow arm/disarm bomb.
    /// </summary>
    [DataField]
    public string EnteredCode = "";

    /// <summary>
    /// Time at which the last nuke code was entered.
    /// Used to apply a cooldown to prevent clients from attempting to brute force the nuke code by sending keypad messages every tick.
    /// <seealso cref="NukeComponent.EnterCodeCooldown"/>
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCodeEnteredAt = TimeSpan.Zero;

    /// <summary>
    /// Current status of a nuclear bomb.
    /// </summary>
    [DataField]
    public NukeStatus Status = NukeStatus.AWAIT_DISK;

    /// <summary>
    /// Check if nuke has already played the nuke song so we don't do it again
    /// </summary>
    [DataField]
    public bool PlayedNukeSong;

    /// <summary>
    /// Check if nuke has already played last alert sound
    /// </summary>
    [DataField]
    public bool PlayedAlertSound;

    public EntityUid? AlertAudioStream = default;

    /// <summary>
    /// The radius from the nuke for which there must be floor tiles for it to be anchorable.
    /// </summary>
    [DataField]
    public float RequiredFloorRadius = 5;
}
