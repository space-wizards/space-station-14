using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds " in front of and after the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class QuoteMainMessageChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        var quoteNode = new MarkupNode("\"");
        message.InsertBeforeTag(quoteNode, "MainMessage");
        message.InsertAfterTag(quoteNode, "MainMessage");
        return message;
    }
}
