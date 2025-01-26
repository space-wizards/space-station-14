using Robust.Shared.Utility;

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

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        message.InsertOutsideTag(new MarkupNode("BubbleHeader", new MarkupParameter((int)SpeechType), null), "EntityNameHeader");
        message.InsertOutsideTag(new MarkupNode("BubbleMessage", new MarkupParameter((int)SpeechType), null), "MainMessage");
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
