using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps a [MainMessage] tag around all text nodes, which can later be utilized by other chat modifiers.
/// Should be included around a message fairly early on in its modification to indicate what text was provided by the source.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class MainMessageChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertAroundText(new MarkupNode("MainMessage", null, null));
        return message;
    }
}
