using System.Threading;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Explosion;
using Content.Shared.Nuke;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Nuke
{
    /// <summary>
    ///     Nuclear device that can devastate an entire station.
    ///     Basically a station self-destruction mechanism.
    ///     To activate it, user needs to insert an authorization disk and enter a secret code.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(NukeSystem))]
    public sealed partial class NukeComponent : SharedNukeComponent
    {
        /// <summary>
        ///     Default bomb timer value in seconds.
        /// </summary>
        [DataField("timer")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Timer = 300;

        /// <summary>
        ///     If the nuke is disarmed, this sets the minimum amount of time the timer can have.
        ///     The remaining time will reset to this value if it is below it.
        /// </summary>
        [DataField]
        public int MinimumTime = 180;

        /// <summary>
        ///     How long until the bomb can arm again after deactivation.
        ///     Used to prevent announcements spam.
        /// </summary>
        [DataField("cooldown")]
        public int Cooldown = 30;

        /// <summary>
        ///     The <see cref="ItemSlot"/> that stores the nuclear disk. The entity whitelist, sounds, and some other
        ///     behaviours are specified by this <see cref="ItemSlot"/> definition. Make sure the whitelist, is correct
        ///     otherwise a blank bit of paper will work as a "disk".
        /// </summary>
        [DataField("diskSlot")]
        public ItemSlot DiskSlot = new();

        /// <summary>
        ///     When this time is left, nuke will play last alert sound
        /// </summary>
        [DataField("alertTime")]
        public float AlertSoundTime = 10.0f;

        /// <summary>
        ///     How long a user must wait to disarm the bomb.
        /// </summary>
        [DataField("disarmDoafterLength")]
        public float DisarmDoafterLength = 30.0f;

        [DataField("alertLevelOnActivate")] public string AlertLevelOnActivate = default!;
        [DataField("alertLevelOnDeactivate")] public string AlertLevelOnDeactivate = default!;

        /// <summary>
        ///     This is stored so we can do a funny by making 0 shift the last played note up by 12 semitones (octave)
        /// </summary>
        public int LastPlayedKeypadSemitones = 0;

        [DataField("keypadPressSound")]
        public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

        [DataField("accessGrantedSound")]
        public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

        [DataField("accessDeniedSound")]
        public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

        [DataField("alertSound")]
        public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Machines/Nuke/nuke_alarm.ogg");

        [DataField("armSound")]
        public SoundSpecifier ArmSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

        [DataField("disarmSound")]
        public SoundSpecifier DisarmSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

        [DataField("armMusic")]
        public SoundSpecifier ArmMusic = new SoundCollectionSpecifier("NukeMusic");

        // These datafields here are duplicates of those in explosive component. But I'm hesitant to use explosive
        // component, just in case at some point, somehow, when grenade crafting added in someone manages to wire up a
        // proximity trigger or something to the nuke and set it off prematurely. I want to make sure they MEAN to set of
        // the nuke.
        #region ExplosiveComponent
        /// <summary>
        ///     The explosion prototype. This determines the damage types, the tile-break chance, and some visual
        ///     information (e.g., the light that the explosion gives off).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("explosionType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ExplosionPrototype>))]
        public string ExplosionType = default!;

        /// <summary>
        ///     The maximum intensity the explosion can have on a single time. This limits the maximum damage and tile
        ///     break chance the explosion can achieve at any given location.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxIntensity")]
        public float MaxIntensity = 100;

        /// <summary>
        ///     How quickly the intensity drops off as you move away from the epicenter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intensitySlope")]
        public float IntensitySlope = 5;

        /// <summary>
        ///     The total intensity of this explosion. The radius of the explosion scales like the cube root of this
        ///     number (see <see cref="ExplosionSystem.RadiusToIntensity"/>).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("totalIntensity")]
        public float TotalIntensity = 100000;

        /// <summary>
        ///     Avoid somehow double-triggering this explosion.
        /// </summary>
        public bool Exploded;
        #endregion

        /// <summary>
        ///     Origin station of this bomb, if it exists.
        ///     If this doesn't exist, then the origin grid and map will be filled in, instead.
        /// </summary>
        public EntityUid? OriginStation;

        /// <summary>
        ///     Origin map and grid of this bomb.
        ///     If a station wasn't tied to a given grid when the bomb was spawned,
        ///     this will be filled in instead.
        /// </summary>
        public (MapId, EntityUid?)? OriginMapGrid;

        [DataField("codeLength")] public int CodeLength = 6;
        [ViewVariables] public string Code = string.Empty;

        /// <summary>
        ///     Time until explosion in seconds.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float RemainingTime;

        /// <summary>
        ///     Time until bomb cooldown will expire in seconds.
        /// </summary>
        [ViewVariables]
        public float CooldownTime;

        /// <summary>
        ///     Current nuclear code buffer. Entered manually by players.
        ///     If valid it will allow arm/disarm bomb.
        /// </summary>
        [ViewVariables]
        public string EnteredCode = "";

        /// <summary>
        ///     Current status of a nuclear bomb.
        /// </summary>
        [ViewVariables]
        public NukeStatus Status = NukeStatus.AWAIT_DISK;

        /// <summary>
        ///     Check if nuke has already played the nuke song so we don't do it again
        /// </summary>
        public bool PlayedNukeSong = false;

        /// <summary>
        ///     Check if nuke has already played last alert sound
        /// </summary>
        public bool PlayedAlertSound = false;

        public EntityUid? AlertAudioStream = default;

        /// <summary>
        ///     The radius from the nuke for which there must be floor tiles for it to be anchorable.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requiredFloorRadius")]
        public float RequiredFloorRadius = 5;
    }
}
