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
    public void SendDeadChatMessage(DeadChatCreatedEvent ev)
    {
        SendDeadChatMessage(ev.Speaker, ev.Message, ev.Id);
    }

    public void SendDeadChatMessage(EntityUid source, string message, uint id = 0)
    {
        if (!_playerManager.TryGetSessionByEntity(source, out var player))
        {
            return;
        }

        message = SanitizeMessage(message);

        var isAdmin = _admin.IsAdmin(source);
        var asName = SanitizeName(Identity.Name(source, EntityManager), CurrentCultureIsSomeFormOfEnglish);

        var msgOut = new DeadChatEvent(GetNetEntity(source), isAdmin ? player.Channel.UserName : asName, message, isAdmin, id);

        foreach (var session in GetDeadChatRecipients())
        {
            RaiseNetworkEvent(msgOut, session);
        }

        _replay.RecordServerMessage(msgOut);

        LogMessage(source, "dead chat", id, isAdmin ? "admin" : "player", asName, message);
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
