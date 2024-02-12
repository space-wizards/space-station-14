using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Radio;

namespace Content.Client.Chat.V2;

public sealed partial class ChatSystem
{
    public bool SendRadioMessage(EntityUid speaker, string message, RadioChannelPrototype radioChannel, [NotNullWhen(false)] out string? reason)
    {
        // Sanity check: if you can't chat you shouldn't be chatting.
        if (!TryComp<RadioableComponent>(speaker, out var radioable))
        {
            // TODO: Add locstring
            reason = "You can't talk on any radio channel.";

            return false;
        }

        // Using LINQ here, pls don't murder me PJB 🙏
        if (!radioable.Channels.Contains(radioChannel.ID))
        {
            // TODO: Add locstring
            reason = $"You can't talk on the {radioChannel.ID} radio channel.";

            return false;
        }

        var messageMaxLen = _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

        if (message.Length > messageMaxLen)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", messageMaxLen));

            return false;
        }

        RaiseNetworkEvent(new RadioAttemptedEvent(GetNetEntity(speaker), message, radioChannel.ID));

        reason = null;

        return true;
    }
}
