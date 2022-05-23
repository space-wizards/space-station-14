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

        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);
    }
}
