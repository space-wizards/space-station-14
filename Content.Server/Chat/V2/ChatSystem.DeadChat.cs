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
        SubscribeNetworkEvent<DeadChatAttemptedEvent>((msg, args) => { HandleAttemptDeadChatMessage(args.SenderSession, msg.Speaker, msg.Message); });
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
            RaiseNetworkEvent(new DeadChatAttemptFailedEvent(GetNetEntity(entityUid),reason), entityUid);

            return;
        }

        if (_admin.IsAdmin(entityUid) && !HasComp<GhostComponent>(entityUid) || !_mobState.IsDead(entityUid))
        {
            RaiseNetworkEvent(new DeadChatAttemptFailedEvent(entity, "If you'd like to talk to the dead, consider dying first."));

            return;
        }

        if (message.Length > MaxChatMessageLength)
        {
            RaiseNetworkEvent(
                new DeadChatAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxChatMessageLength))
                ),
                player);

            return;
        }

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
