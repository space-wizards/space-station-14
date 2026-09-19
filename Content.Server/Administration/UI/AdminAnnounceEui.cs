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

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Moderator))
        {
            Close();
            return;
        }

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var announcement = SharedChatSystem.SanitizeAnnouncement(doAnnounce.Announcement, maxLength);

        if (string.IsNullOrWhiteSpace(announcement))
            return;

        switch (doAnnounce.AnnounceType)
        {
            case AdminAnnounceType.Server:
                AnnounceServer(doAnnounce, announcement);
                break;
            case AdminAnnounceType.Station:
                AnnounceStation(doAnnounce, announcement, maxLength);
                break;
        }

        if (doAnnounce.CloseAfter)
            Close();
    }

    private void AnnounceServer(AdminAnnounceEuiMsg.DoAnnounce msg, string announcement)
    {
        var color = AdminAnnounceHelpers.GetColor(msg.AnnounceType, msg.ColorHex);
        _chatManager.DispatchServerAnnouncement(announcement, color);
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{Player.Name} has sent the following server announcement: {announcement}");
    }

    private void AnnounceStation(AdminAnnounceEuiMsg.DoAnnounce msg, string announcement, int maxLength)
    {
        var announcer = GetAnnouncer(msg.Announcer);
        var sound = GetSound(msg.SoundPath);
        var color = AdminAnnounceHelpers.GetColor(msg.AnnounceType, msg.ColorHex);
        var sender = SharedChatSystem.SanitizeAnnouncement(msg.Sender, maxLength);
        var finalContent = FormatAnnouncement(announcement, sender);

        switch (msg.Scope)
        {
            case AdminAnnounceScope.Global:
                _chatSystem.DispatchGlobalAnnouncement(
                    finalContent,
                    announcer,
                    colorOverride: color,
                    playSound: true,
                    announcementSound: sound);

                LogAnnouncement("global", announcer, announcement);
                break;
            case AdminAnnounceScope.Map:
                if (!TryGetPlayerMap(out var mapId))
                    return;

                var filter = Filter.BroadcastMap(mapId);
                if (filter.Count == 0)
                    return;

                _chatSystem.DispatchFilteredAnnouncement(
                    filter,
                    finalContent,
                    sender: announcer,
                    playSound: true,
                    announcementSound: sound,
                    colorOverride: color);

                LogAnnouncement($"map {mapId}", announcer, announcement);
                break;
        }
    }

    private string GetAnnouncer(string? announcer)
    {
        var normalized = AdminAnnounceHelpers.NormalizeText(announcer);
        return string.IsNullOrWhiteSpace(normalized)
            ? Loc.GetString("admin-announce-announcer-default")
            : normalized;
    }

    private SoundSpecifier GetSound(string? soundPath)
    {
        var normalized = AdminAnnounceHelpers.NormalizeSoundPath(soundPath);
        if (!string.IsNullOrEmpty(normalized) && _res.ContentFileExists(normalized))
            return new SoundPathSpecifier(normalized);

        return SharedChatSystem.DefaultAnnouncementSound;
    }

    private bool TryGetPlayerMap(out MapId mapId)
    {
        mapId = MapId.Nullspace;
        if (Player.AttachedEntity is not { } entity ||
            !_entityManager.TryGetComponent(entity, out TransformComponent? xform) ||
            xform.MapID == MapId.Nullspace)
        {
            return false;
        }

        mapId = xform.MapID;
        return true;
    }

    private string FormatAnnouncement(string announcement, string? sender)
    {
        return AdminAnnounceHelpers.HasSender(sender)
            ? $"{announcement}\n{Loc.GetString("admin-announce-sent-by")} {AdminAnnounceHelpers.NormalizeText(sender)}"
            : announcement;
    }

    private void LogAnnouncement(string scope, string announcer, string announcement)
    {
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{Player.Name} has sent the following {scope} announcement as {announcer}: {announcement}");
    }
}
