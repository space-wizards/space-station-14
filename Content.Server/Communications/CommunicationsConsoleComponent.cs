using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public sealed class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        public TimeSpan LastAnnouncementTime;

        /// <summary>
        /// Fluent ID for the announcement title
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("announcementTitle", required: true)]
        public string AnnouncementDisplayName = "communicationsconsole-announcement-title";
        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("announcementDelay")]
        public int DelayBetweenAnnouncements = 90;
        /// <summary>
        /// Disable altering the station alert level (for syndicate comms consoles and such)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canAlterAlertLevel")]
        public bool CanAlterAlertLevel = false;

        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);
    }
}
