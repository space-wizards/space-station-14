using System.Linq;
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
    public void InitializeServerDeadChat()
    {
        SubscribeLocalEvent<DeadChatCreatedEvent>((msg, _) => { SendDeadChatMessage(msg.Speaker, msg.Message); });
    }

    public void SendDeadChatMessage(EntityUid source, string message)
    {
        if (!_playerManager.TryGetSessionByEntity(source, out var player))
        {
            return;
        }

        message = SanitizeMessage(message);

        var isAdmin = _admin.IsAdmin(source);

        var msgOut = new DeadChatEvent(GetNetEntity(source), isAdmin ? player.Channel.UserName : SanitizeName(Identity.Name(source, EntityManager), UseEnglishGrammar), message, isAdmin);

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
