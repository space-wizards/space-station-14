using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nuke
{
    /// <summary>
    ///     Nuclear device that can devistate an entire station.
    ///     Basicaly a station self-destruction mechanism.
    ///     To activate it, user needs to insert an authorization disk and enter a secret code.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(NukeSystem))]
    public class NukeComponent : Component
    {
        public override string Name => "Nuke";

        /// <summary>
        ///     Default bomb timer value in seconds.
        /// </summary>
        [DataField("timer")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Timer = 180;

        /// <summary>
        ///     Slot name for to store nuclear disk inside bomb.
        ///     See <see cref="SharedItemSlotsComponent"/> for mor info.
        /// </summary>
        [DataField("slot")]
        public string DiskSlotName = "DiskSlot";

        /// <summary>
        ///     Annihilation radius in which  all human players will be gibed
        /// </summary>
        [DataField("blastRadius")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int BlastRadius = 200;

        /// <summary>
        ///     After this time nuke will play last alert sound
        /// </summary>
        [DataField("alertTime")]
        public float AlertSoundTime = 10.0f;

        [DataField("keypadPressSound")]
        public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

        [DataField("accessGrantedSound")]
        public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

        [DataField("accessDeniedSound")]
        public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

        [DataField("alertSound")]
        public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

        [DataField("armSound")]
        public SoundSpecifier ArmSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

        [DataField("disarmSound")]
        public SoundSpecifier DisarmSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

        /// <summary>
        ///     Time until explosion in seconds.
        /// </summary>
        [ViewVariables]
        public float RemainingTime;

        /// <summary>
        ///     Does bomb contains valid entity inside <see cref="DiskSlotName"/>?
        ///     If it is, user can anchor bomb or enter nuclear code to arm it.
        /// </summary>
        [ViewVariables]
        public bool DiskInserted = false;

        /// <summary>
        ///     Curent nuclear code buffer. Entered manually by players.
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
        ///     Check if nuke has already played last alert sound
        /// </summary>
        public bool PlayedAlertSound = false;

        public IPlayingAudioStream? AlertAudioStream = default;
    }
}
