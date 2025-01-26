using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds " in front of and after the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class QuoteMainMessageChatModifier : ChatModifier
{
    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        var quoteNode = new MarkupNode("\"");
        message.InsertBeforeTag(quoteNode, "MainMessage");
        message.InsertAfterTag(quoteNode, "MainMessage");
    }
}
