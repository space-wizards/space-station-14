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

        private readonly ChatSystem _chat;

        public AdminAnnounceEui()
        {
            IoCManager.InjectDependencies(this);
            _chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
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

            var hex = doAnnounce.ColorHex.Trim();
            var fallbackHex = doAnnounce.AnnounceType == AdminAnnounceType.Server 
                ? AdminAnnounceDefaults.ServerColorHex 
                : AdminAnnounceDefaults.DefaultColorHex;

            var color = Color.TryFromHex(hex) ?? Color.FromHex(fallbackHex);

            switch (doAnnounce.AnnounceType)
            {
                case AdminAnnounceType.Server:
                    _chatManager.DispatchServerAnnouncement(announcement, color);
                    break;
                // TODO: Per-station announcement support
                case AdminAnnounceType.Station:
                    var announcer = string.IsNullOrWhiteSpace(doAnnounce.Announcer)
                        ? Loc.GetString("admin-announce-announcer-default")
                        : doAnnounce.Announcer.Trim();

                    var sound = SharedChatSystem.DefaultAnnouncementSound;
                    var soundPath = doAnnounce.SoundPath.Trim();
                    
                    if (!string.IsNullOrEmpty(soundPath) && _res.ContentFileExists(soundPath))
                        sound = new SoundPathSpecifier(soundPath);

                    var finalContent = AdminAnnounceHelpers.FormatAnnouncement(announcement, doAnnounce.Sender);

                    _chat.DispatchGlobalAnnouncement(
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
