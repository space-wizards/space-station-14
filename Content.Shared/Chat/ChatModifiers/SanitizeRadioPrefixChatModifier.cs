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
        if (!message.Nodes.TryFirstOrDefault(x => x.Name == null, out var firstTextNode))
            return message;

        using var nodeEnumerator = message.GetEnumerator();

        var returnMessage = new FormattedMessage();
        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;
            if (node == firstTextNode)
            {
                if (firstTextNode.Value.TryGetString(out var textNodeValue))
                {
                    returnMessage.AddText(SanitizeRadioPrefix(textNodeValue));
                }
            }
            else
            {
                if (!node.Closing)
                    returnMessage.PushTag(node);
                else
                    returnMessage.Pop();
            }
        }

        return returnMessage;
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
