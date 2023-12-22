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
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnnouncementCooldownRemaining;

        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public string Title = "comms-console-announcement-title-station";

        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public Color Color = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public int Delay = 90;

        /// <summary>
        /// Can call or recall the shuttle
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool CanShuttle = true;

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
		[DataField]
        public bool Global = false;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");
    }
}
