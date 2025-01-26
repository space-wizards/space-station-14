using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps a [MainMessage] tag around all text nodes, which can later be utilized by other chat modifiers.
/// The tag also adds a space before the message once processed.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class MainMessageChatModifier : ChatModifier
{
    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        message.InsertAroundText(new MarkupNode("MainMessage", null, null));
    }
}
