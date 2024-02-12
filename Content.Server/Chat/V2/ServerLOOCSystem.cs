using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed class ServerLOOCSystem : EntitySystem
{
    [Dependency] private readonly IChatRateLimiter _rateLimiter = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IServerLoocManager _loocManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly ServerDeadChatSystem _deadChat = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A client attempts to chat using a given entity
        SubscribeNetworkEvent<LoocAttemptedEvent>((msg, args) => { HandleAttemptChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
    }

    private void HandleAttemptChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);

        if (player.AttachedEntity != entityUid)
        {
            // Nice try bozo.
            return;
        }

        // Are they rate-limited
        if (IsRateLimited(entityUid))
        {
            return;
        }

        var maxMessageLen = _configuration.GetCVar(CCVars.ChatMaxMessageLength);

        // Is the message too long?
        if (message.Length > _configuration.GetCVar(CCVars.ChatMaxMessageLength))
        {
            RaiseNetworkEvent(
                new LoocAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", maxMessageLen))
                ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendLoocMessage(entityUid, message);
    }

    public void SendLoocMessage(EntityUid source,string message)
    {
        message = SanitizeMessage(message);

        // If dead player LOOC is disabled, unless you are an aghost, send dead messages to dead chat
        if (!_admin.IsAdmin(source) && !_loocManager.DeadLoocEnabled &&
            (HasComp<GhostComponent>(source) || _mobState.IsDead(source)))
            _deadChat.SendDeadChatMessage(source, message);

        // If crit player LOOC is disabled, don't send the message at all.

        if (!_loocManager.CritLoocEnabled && _mobState.IsCritical(source))
            return;

        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (!_admin.IsAdmin(source) && !_loocManager.LoocEnabled)
            return;

        var msgOut = new EntityLoocedEvent(
            GetNetEntity(source),
            name,
            message,
            IServerLoocManager.LoocVoiceRange
        );

        // Now fire it off to legal recipients
        foreach (var session in GetRecipients(source))
        {
            RaiseNetworkEvent(msgOut, session);
        }

        if (_playerManager.TryGetSessionByEntity(source, out var commonSession))
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {commonSession:Player}: {message}");
        }

        _replay.RecordServerMessage(msgOut);
    }

    private bool IsRateLimited(EntityUid entityUid)
    {
        if (!_rateLimiter.IsRateLimited(entityUid, out var reason))
            return false;

        if (!string.IsNullOrEmpty(reason))
        {
            RaiseNetworkEvent(new LoocAttemptFailedEvent(GetNetEntity(entityUid),reason), entityUid);
        }

        return true;
    }

    private string SanitizeMessage(string message)
    {
        return message.Trim();
    }


    private List<ICommonSession> GetRecipients(EntityUid source)
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
            if (ghostHearing.HasComponent(playerEntity) || sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) && distance < IServerLoocManager.LoocVoiceRange)
                recipients.Add(player);
        }

        return recipients;
    }
}
