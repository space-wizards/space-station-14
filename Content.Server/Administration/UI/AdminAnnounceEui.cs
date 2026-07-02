using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Eui;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Administration.UI;

public sealed partial class AdminAnnounceEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    private readonly ChatSystem _chatSystem;

    public AdminAnnounceEui()
    {
        IoCManager.InjectDependencies(this);

        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        _chatSystem = sysMan.GetEntitySystem<ChatSystem>();
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

        var color = AdminAnnounceHelpers.GetColor(doAnnounce.AnnounceType, doAnnounce.ColorHex);

        switch (doAnnounce.AnnounceType)
        {
            case AdminAnnounceType.Server:
                _chatManager.DispatchServerAnnouncement(announcement, color);
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"{Player.Name} has sent the following server announcement: {announcement}");
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

                var sender = SharedChatSystem.SanitizeAnnouncement(doAnnounce.Sender, maxLength);
                var finalContent = AdminAnnounceHelpers.FormatAnnouncement(announcement, sender);

                MapId? adminMapId = null;
                if (Player.AttachedEntity is { } adminEntity
                    && _entityManager.TryGetComponent<TransformComponent>(adminEntity, out var adminXform))
                {
                    adminMapId = adminXform.MapID;
                }

                var mapId = adminMapId ?? MapId.Nullspace;
                var sentPerMap = false;

                if (!doAnnounce.Global)
                {
                    if (mapId == MapId.Nullspace)
                        return;

                    var mapFilter = GetPlayersOnMap(mapId);

                    if (!mapFilter.Recipients.Any())
                        return;

                    _chatSystem.DispatchFilteredAnnouncement(
                        mapFilter,
                        finalContent,
                        sender: announcer,
                        playSound: true,
                        announcementSound: sound,
                        colorOverride: color
                    );

                    sentPerMap = true;
                }

                if (!sentPerMap)
                {
                    _chatSystem.DispatchGlobalAnnouncement(
                        finalContent,
                        announcer,
                        colorOverride: color,
                        playSound: true,
                        announcementSound: sound
                    );
                }

                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"{Player.Name} has sent the following {(sentPerMap ? "map" : "global")} announcement as {announcer}: {announcement}");
                break;
        }

        if (doAnnounce.CloseAfter)
            Close();
    }

    private Filter GetPlayersOnMap(MapId mapId)
    {
        var filter = Filter.Empty();
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is { } entity
                && _entityManager.TryGetComponent<TransformComponent>(entity, out var xform)
                && xform.MapID == mapId)
            {
                filter.AddPlayer(session);
            }
        }
        return filter;
    }
}
