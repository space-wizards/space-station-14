using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public bool LoocEnabled { get; private set; } = true;
    public bool DeadLoocEnabled { get; private set; }
    public bool CritLoocEnabled { get; private set; }

    public void SendLoocMessage(LoocCreatedEvent ev)
    {
        SendLoocMessage(ev.Speaker, ev.Message, id: ev.Id);
    }

    public void SendLoocMessage(EntityUid source, string message, uint id = 0)
    {
        message = SanitizeMessage(message);

        if (!_admin.IsAdmin(source) && !DeadLoocEnabled &&
            (HasComp<GhostComponent>(source) || _mobState.IsDead(source)))
            SendDeadChatMessage(source, message);

        if (!CritLoocEnabled && _mobState.IsCritical(source))
            return;

        var name = SanitizeName(Identity.Name(source, EntityManager), CurrentCultureIsSomeFormOfEnglish);

        if (!_admin.IsAdmin(source) && !LoocEnabled)
            return;

        var range = Configuration.GetCVar(CCVars.LoocRange);

        var msgOut = new LoocEvent(GetNetEntity(source), name, message, id);

        foreach (var session in GetLoocRecipients(source, range))
        {
            RaiseNetworkEvent(msgOut, session);
        }

        if (_playerManager.TryGetSessionByEntity(source, out var commonSession))
            LogMessage(source, "looc chat", id, "local", name, message);

        _replay.RecordServerMessage(msgOut);
    }

    private List<ICommonSession> GetLoocRecipients(EntityUid source, float range)
    {
        var recipients = new List<ICommonSession>();

        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            // even if they are a ghost hearer, in some situations we still need the range
            if (ghostHearing.HasComponent(playerEntity) || sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) && distance < range)
                recipients.Add(player);
        }

        return recipients;
    }

    private void OnLoocEnabledChanged(bool val)
    {
        if (LoocEnabled == val)
            return;

        LoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (DeadLoocEnabled == val)
            return;

        DeadLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-dead-looc-chat-enabled-message" : "chat-manager-dead-looc-chat-disabled-message"));
    }

    private void OnCritLoocEnabledChanged(bool val)
    {
        if (CritLoocEnabled == val)
            return;

        CritLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-crit-looc-chat-enabled-message" : "chat-manager-crit-looc-chat-disabled-message"));
    }
}
