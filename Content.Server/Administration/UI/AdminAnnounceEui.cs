using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Eui;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server.Administration.UI
{
    public sealed class AdminAnnounceEui : BaseEui
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _res = default!;

        private readonly ChatSystem _chatSystem;

        public AdminAnnounceEui()
        {
            IoCManager.InjectDependencies(this);
            _chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
        }

        public override EuiStateBase GetNewState() => new AdminAnnounceEuiState();

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AdminAnnounceEuiMsg.DoAnnounce doAnnounce)
                return;

            if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
            {
                Close();
                return;
            }

            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var announcement = SharedChatSystem.SanitizeAnnouncement(doAnnounce.Announcement, maxLength);
            
            if (string.IsNullOrWhiteSpace(announcement))
                return;

            var colorHex = AdminAnnounceHelpers.GetValidatedColorHex(doAnnounce.AnnounceType, doAnnounce.ColorHex);
            var color = Color.FromHex(colorHex);

            switch (doAnnounce.AnnounceType)
            {
                case AdminAnnounceType.Server:
                    _chatManager.DispatchServerAnnouncement(announcement, color);
                    break;
                // TODO: Per-station announcement support
                case AdminAnnounceType.Station:
                    var normalizedAnnouncer = AdminAnnounceHelpers.NormalizeText(doAnnounce.Announcer);
                    var announcer = string.IsNullOrWhiteSpace(normalizedAnnouncer)
                        ? Loc.GetString("admin-announce-announcer-default")
                        : normalizedAnnouncer;

                    var sound = SharedChatSystem.DefaultAnnouncementSound;
                    var soundPath = AdminAnnounceHelpers.NormalizeSoundPath(doAnnounce.SoundPath);
                    
                    if (!string.IsNullOrEmpty(soundPath) && _res.ContentFileExists(soundPath))
                        sound = new SoundPathSpecifier(soundPath);

                    var finalContent = AdminAnnounceHelpers.FormatAnnouncement(announcement, doAnnounce.Sender);

                    _chatSystem.DispatchGlobalAnnouncement(
                        finalContent,
                        announcer,
                        colorOverride: color,
                        playSound: true,
                        announcementSound: sound
                    );
                    break;
            }

            if (doAnnounce.CloseAfter)
                Close();
        }
    }
}
