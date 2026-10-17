using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAnnounce;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;

namespace Content.Server.Administration.UI;

public sealed partial class AdminAnnounceEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceManager _resourceManager = default!;

    private readonly ChatSystem _chatSystem;
    private readonly SharedMapSystem _mapSystem;

    public AdminAnnounceEui()
    {
        IoCManager.InjectDependencies(this);

        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        _chatSystem = sysMan.GetEntitySystem<ChatSystem>();
        _mapSystem = sysMan.GetEntitySystem<SharedMapSystem>();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AdminAnnounceEuiMsg.DoAnnounce announce)
            return;

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Moderator))
        {
            Close();
            return;
        }

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var message = SharedChatSystem.SanitizeAnnouncement(announce.Announcement, maxLength);

        if (string.IsNullOrWhiteSpace(message))
            return;

        switch (announce.AnnounceType)
        {
            case AdminAnnounceType.Server:
                AnnounceServer(announce, message);
                break;

            case AdminAnnounceType.Station:
                AnnounceStation(announce, message, maxLength);
                break;
        }

        if (announce.CloseAfter)
            Close();
    }

    private void AnnounceServer(AdminAnnounceEuiMsg.DoAnnounce announce, string message)
    {
        _chatManager.DispatchServerAnnouncement(message, announce.Color, sender: Player);
    }

    private void AnnounceStation(
        AdminAnnounceEuiMsg.DoAnnounce announce,
        string message,
        int maxLength)
    {
        var announcer = announce.Announcer.Trim();
        var signature = SharedChatSystem.SanitizeAnnouncement(announce.Signature, maxLength);
        var sound = ValidateSound(announce.Sound);

        if (announce.MapId is not { } mapId)
        {
            _chatSystem.DispatchGlobalAnnouncement(
                message,
                announcer,
                playSound: sound != null,
                announcementSound: sound,
                colorOverride: announce.Color,
                signature: signature,
                actor: Player);
            return;
        }

        if (!_mapSystem.MapExists(mapId))
            return;

        _chatSystem.DispatchFilteredAnnouncement(
            Filter.BroadcastMap(mapId),
            message,
            sender: announcer,
            playSound: sound != null,
            announcementSound: sound,
            colorOverride: announce.Color,
            signature: signature,
            actor: Player);
    }

    private SoundPathSpecifier? ValidateSound(SoundPathSpecifier? sound)
    {
        return AudioHelpers.IsValidContentSound(sound, _resourceManager)
            ? sound
            : null;
    }
}
