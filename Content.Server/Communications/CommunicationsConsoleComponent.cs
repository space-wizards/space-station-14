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
        [ViewVariables]
        public float AnnouncementCooldownRemaining;
        /// <summary>
        /// Has the UI already been refreshed after the announcement
        /// </summary>
        [ViewVariables]
        public bool AlreadyRefreshed = false;

        /// <summary>
        /// Fluent ID for the announcement title
        /// </summary>
        [DataField("title", required: true)]
        public string AnnouncementDisplayName = "communicationsconsole-announcement-title";

        /// <summary>
        /// Announcement color
        /// </summary>
        [DataField("color")]
        public Color AnnouncementColor = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public int DelayBetweenAnnouncements = 10;

        /// <summary>
        /// Can call or recall the shuttle
        /// </summary>
        [DataField("canShuttle")]
        public bool CanCallShuttle = true;

        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);
    }
}
