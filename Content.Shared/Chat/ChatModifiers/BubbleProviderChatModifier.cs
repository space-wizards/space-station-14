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

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        return InsertOutsideTag(InsertOutsideTag(message, new MarkupNode("BubbleHeader", new MarkupParameter((int)SpeechType), null), "EntityNameHeader"), new MarkupNode("BubbleMessage", null, null), "MainMessage");
    }
}

// This enum needs to be merged with the one in SpeechBubble.cs
public enum SpeechType : byte
{
    Emote,
    Say,
    Whisper,
    Looc
}
