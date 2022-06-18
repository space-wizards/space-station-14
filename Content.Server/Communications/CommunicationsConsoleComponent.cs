using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public sealed class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        /// <summary>
        /// Remaining cooldown between making announcements.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnnouncementCooldownRemaining;

        /// <summary>
        /// Has the UI already been refreshed after the announcement
        /// </summary>
        public bool AlreadyRefreshed = false;

        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("title", required: true)]
        public string AnnouncementDisplayName = "comms-console-announcement-title-station";

        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("color")]
        public Color AnnouncementColor = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public int DelayBetweenAnnouncements = 90;

        /// <summary>
        /// Can call or recall the shuttle
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canShuttle")]
        public bool CanCallShuttle = true;

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [DataField("global")]
        public bool AnnounceGlobal = false;

        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);
    }
}
