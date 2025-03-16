using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Removes any radio chat prefixes from the first text node.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SanitizeRadioPrefixChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        foreach (var messageNode in message.Nodes)
        {
            if (messageNode.Name == null && messageNode.Value.TryGetString(out var textNodeValue))
            {
                message.ReplaceTextNode(messageNode, new MarkupNode(SanitizeRadioPrefix(textNodeValue)));
                break;
            }
        }

        return message;
    }

    private string SanitizeRadioPrefix(string text)
    {
        // Common radio is only ";" which means only the first symbol needs to be removed.
        if (text.StartsWith(SharedChatSystem.RadioCommonPrefix))
        {
            return text[1..].TrimStart();
        }

        // If the channel starts with a non-common radio channel prefix, assume the first two symbols are radio prefixes (e.g. ":s").
        if (text.StartsWith(SharedChatSystem.RadioChannelPrefix) ||
            text.StartsWith(SharedChatSystem.RadioChannelAltPrefix))
        {
            // Not exactly sure what this does but seems to cover situations where only the prefix symbol is given.
            if (text.Length < 2 || char.IsWhiteSpace(text[1]))
            {
                return text[1..].TrimStart();
            }

            return text[2..].TrimStart();
        }

        return text;
    }
}
