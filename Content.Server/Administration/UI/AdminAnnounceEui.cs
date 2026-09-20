using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAnnounce;
using Content.Shared.Chat;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;

namespace Content.Server.Administration.UI;

public sealed partial class AdminAnnounceEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
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
        _chatManager.DispatchServerAnnouncement(message, announce.Color);
        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Low,
            $"{Player:actor} sent a server announcement: {message}");
    }

    private void AnnounceStation(
        AdminAnnounceEuiMsg.DoAnnounce announce,
        string message,
        int maxLength)
    {
        var announcer = announce.Announcer.Trim();
        if (string.IsNullOrEmpty(announcer))
            announcer = Loc.GetString("admin-announce-announcer-default");

        var signature = SharedChatSystem.SanitizeAnnouncement(announce.Signature, maxLength);

        if (!string.IsNullOrWhiteSpace(signature))
        {
            message = Loc.GetString(
                "admin-announce-with-signature",
                ("message", message),
                ("signature", signature));
        }

        var sound = ValidateSound(announce.Sound);

        if (announce.MapId is not { } mapId)
        {
            _chatSystem.DispatchGlobalAnnouncement(
                message,
                announcer,
                playSound: sound != null,
                announcementSound: sound,
                colorOverride: announce.Color);
            _adminLog.Add(
                LogType.AdminCommands,
                LogImpact.Low,
                $"{Player:actor} sent a global station announcement as {announcer}: {message}");
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
            colorOverride: announce.Color);
        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Low,
            $"{Player:actor} sent a station announcement to map {mapId} as {announcer}: {message}");
    }

    private SoundPathSpecifier? ValidateSound(SoundPathSpecifier? sound)
    {
        if (sound == null || !sound.Path.IsRooted || !_resourceManager.ContentFileExists(sound.Path))
            return null;

        return sound;
    }
}
