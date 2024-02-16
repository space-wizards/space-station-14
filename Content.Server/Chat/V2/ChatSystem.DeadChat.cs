using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public void InitializeDeadChat()
    {
        SubscribeNetworkEvent<AttemptDeadChatEvent>((msg, args) => { HandleAttemptDeadChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
    }

    private void HandleAttemptDeadChatMessage(ICommonSession player, NetEntity entity, string message)
    {
        var entityUid = GetEntity(entity);
        if (player.AttachedEntity != entityUid)
        {
            return;
        }

        if (IsRateLimited(entityUid, out var reason))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(entity, reason), player);

            return;
        }

        if (!_admin.IsAdmin(entityUid) || !HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(entity, Loc.GetString("chat-system-dead-chat-failed")), player);

            return;
        }

        if (message.Length > MaxChatMessageLength)
        {
            RaiseNetworkEvent(new DeadChatFailedEvent(entity, Loc.GetString("chat-system-max-message-length-exceeded-message")), player);

            return;
        }

        SendDeadChatMessage(entityUid, message);
    }

    public void SendDeadChatMessage(EntityUid source, string message)
    {
        if (!_playerManager.TryGetSessionByEntity(source, out var player))
        {
            return;
        }

        message = SanitizeMessage(message);

        var isAdmin = _admin.IsAdmin(source);
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        var msgOut = new DeadChatEvent(
            GetNetEntity(source),
            isAdmin ? player.Channel.UserName : FormattedMessage.EscapeText(Identity.Name(source, EntityManager)),
            message,
            _admin.IsAdmin(source)
        );

        RaiseLocalEvent(new DeadChatSuccessEvent(source, name, message, isAdmin));

        foreach (var session in GetDeadChatRecipients())
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

    private IEnumerable<INetChannel> GetDeadChatRecipients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_admin.ActiveAdmins)
            .Select(p => p.Channel);
    }
}

/// <summary>
/// Raised locally when a character speaks in Dead Chat.
/// </summary>
[Serializable]
public sealed class DeadChatSuccessEvent : EntityEventArgs
{
    public EntityUid Speaker;
    public string AsName;
    public readonly string Message;
    public bool IsAdmin;

    public DeadChatSuccessEvent(EntityUid speaker, string asName, string message, bool isAdmin)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        IsAdmin = isAdmin;
    }
}
