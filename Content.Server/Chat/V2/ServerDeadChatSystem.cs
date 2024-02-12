using System.Linq;
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
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed class ServerDeadChatSystem : EntitySystem
{
    [Dependency] private readonly IChatRateLimiter _rateLimiter = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // A client attempts to chat using a given entity
        SubscribeNetworkEvent<DeadChatAttemptedEvent>((msg, args) => { HandleAttemptChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
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

        if (_admin.IsAdmin(entityUid) && !HasComp<GhostComponent>(entityUid) || !_mobState.IsDead(entityUid))
        {
            RaiseNetworkEvent(new DeadChatAttemptFailedEvent(entity, "If you'd like to talk to the dead, consider dying first."));

            return;
        }

        var maxMessageLen = _configuration.GetCVar(CCVars.ChatMaxMessageLength);

        // Is the message too long?
        if (message.Length > _configuration.GetCVar(CCVars.ChatMaxMessageLength))
        {
            RaiseNetworkEvent(
                new DeadChatAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", maxMessageLen))
                ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendDeadChatMessage(entityUid, message);
    }

    public void SendDeadChatMessage(EntityUid source,string message)
    {
        if (!_playerManager.TryGetSessionByEntity(source, out var player))
        {
            return;
        }

        message = SanitizeMessage(message);

        var isAdmin = _admin.IsAdmin(source);
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        var msgOut = new EntityDeadChattedEvent(
            GetNetEntity(source),
            FormattedMessage.EscapeText(Identity.Name(source, EntityManager)),
            message,
            _admin.IsAdmin(source),
            isAdmin ? player.Channel.UserName : name
        );

        // Now fire it off to legal recipients
        foreach (var session in GetRecipients())
        {
            RaiseNetworkEvent(msgOut, session);
        }

        _replay.RecordServerMessage(msgOut);

        if (msgOut.IsAdmin)
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {player:Player}: {message}");
        }
        else
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {player:Player}: {message}");
        }
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

    private IEnumerable<INetChannel> GetRecipients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }
}
