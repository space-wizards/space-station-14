using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Ghost;

namespace Content.Client.Chat.V2;

public sealed partial class ChatSystem : EntitySystem
{
    public bool SendDeadChatMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        if (_admin.IsAdmin(speaker) && !HasComp<GhostComponent>(speaker) || !_mobState.IsDead(speaker))
        {
            reason = "If you'd like to talk to the dead, consider dying first.";

            return false;
        }

        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new DeadChatAttemptedEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}
