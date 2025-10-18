using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Shared.Audio;

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
        [ViewVariables]
        [DataField]
        public Color Color = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public int Delay = 90;

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
        public bool CanShuttle = true;

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [DataField]
        public bool Global = false;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");

        /// <summary>
        /// Hides the sender identity (If they even have one).
        /// In practise this removes the "Sent by ScugMcWawa (Slugcat Captain)" at the bottom of the announcement.
        /// </summary>
        [DataField]
        public bool AnnounceSentBy = true;
    }
}
