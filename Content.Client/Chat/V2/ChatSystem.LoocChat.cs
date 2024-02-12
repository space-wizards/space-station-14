using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;

namespace Content.Client.Chat.V2;

public sealed partial class ChatSystem
{
    public bool SendLoocChatMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new LoocAttemptedEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}
