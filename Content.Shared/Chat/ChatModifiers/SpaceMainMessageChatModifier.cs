using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

// CHAT-TODO: This kind of minor thing makes the yaml a bit ugly. Might be better to find another solution.
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
