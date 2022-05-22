using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public sealed class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        public TimeSpan LastAnnouncementTime;

        [DataField("announcementTitle", required: true)]
        public string? AnnouncementDisplayName;
        [DataField("announcementDelay")]
        public TimeSpan DelayBetweenAnnouncements = TimeSpan.FromSeconds(90);
        [DataField("canAlterAlertLevel")]
        public bool CanAlterAlertLevel = false;

        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);
    }
}
