using Robust.Shared.Utility;
using static Content.Shared.Chat.ChatConstants;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Inserts the required tags to create a chat bubble. Must be included
/// [BubbleHeader] is inserted outside of [EntityNameHeader].
/// [BubbleMessage] is inserted outside of [MainMessage].
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BubbleProviderChatModifier : ChatModifier
{
    [DataField]
    public SpeechType SpeechType = SpeechType.Say;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        message.InsertOutsideTag(new MarkupNode(BubbleHeaderTagName, new MarkupParameter((int)SpeechType), null), "EntityNameHeader");
        message.InsertOutsideTag(new MarkupNode(BubbleBodyTagName, new MarkupParameter((int)SpeechType), null), "MainMessage");
        return message;
    }
}

// CHAT-TODO: This enum needs to be merged with the one in SpeechBubble.cs
public enum SpeechType : byte
{
    Emote,
    Say,
    Whisper,
    Looc
}
