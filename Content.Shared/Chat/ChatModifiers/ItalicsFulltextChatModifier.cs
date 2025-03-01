using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the entire message in an [italic] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ItalicsFulltextChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertAroundMessage(new MarkupNode("italic", null, null));
        return message;
    }
}
