using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds a space in front of the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpaceMainMessageChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertBeforeTag(new MarkupNode(" "), "MainMessage");

        return message;
    }
}
