using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;

namespace Content.Client.Chat.V2;

public sealed partial class ChatSystem
{
    public bool SendEmoteMessage(EntityUid emoter, string message, [NotNullWhen(false)] out string? reason)
    {
        // Sanity check: you might not be able to emote (although this would be unlikely?)
        if (!TryComp<EmoteableComponent>(emoter, out _))
        {
            // TODO: Add locstring
            reason = "You can't emote";

            return false;
        }

        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new EmoteAttemptedEvent(GetNetEntity(emoter), message));

        reason = null;

        return true;
    }
}
