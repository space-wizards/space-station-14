using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds " in front of and after the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class QuoteMainMessageChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        var quoteNode = new MarkupNode("\"");
        return InsertAfterTag(InsertBeforeTag(message, quoteNode, "MainMessage"), quoteNode, "MainMessage");
    }
}
