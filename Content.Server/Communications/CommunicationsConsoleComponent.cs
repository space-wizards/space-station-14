using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Shared.Audio;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public sealed partial class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        public float UIUpdateAccumulator = 0f;

        /// <summary>
        /// Remaining cooldown between making announcements.
        /// </summary>
        [ViewVariables]
        [DataField]
        public float AnnouncementCooldownRemaining;

        [ViewVariables]
        [DataField]
        public float BroadcastCooldownRemaining;

        [ViewVariables]
        [DataField]
        public float CallERTCooldownRemaining;

        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public LocId Title = "comms-console-announcement-title-station";

        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public Color Color = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public int DelayBetweenAnnouncements = 60;

        [ViewVariables]
        [DataField]
        public int DelayBetweenERTCall = 30;

        /// <summary>
        /// Time in seconds of announcement cooldown when a new console is created on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public int InitialDelay = 30;

        /// <summary>
        /// Can call or recall the shuttle
        /// </summary>
        [ViewVariables]
        [DataField]
        public bool CanCallShuttle = true;

        /// <summary>
        /// Can call or recall the ERT
        /// </summary>
        [ViewVariables]
        [DataField]
        public bool CanCallERT = true;

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [DataField]
        public bool Global = false;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier AnnouncementSound = new SoundPathSpecifier("/Audio/_DeadSpace/Announcements/announce.ogg"); // DS14-Announcements

        /// <summary>
        /// Accesses of IDs required to open interactions with the console
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("firstPrivilegedIdAcces")]
        public string FirstPrivilegedIdTargetAccess = "Captain";
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("secondPrivilegedIdAcces")]
        public string SecondPrivilegedIdTargetAccess = "HeadOfSecurity";

        /// <summary>
        /// Slots for two ID cards
        /// </summary>
        [DataField("firstPrivilegedIdSlot")]
        public ItemSlot FirstPrivilegedIdSlot = new();
        [DataField("secondPrivilegedIdSlot")]
        public ItemSlot SecondPrivilegedIdSlot = new();
    }
}
