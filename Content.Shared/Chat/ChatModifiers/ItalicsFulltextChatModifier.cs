using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the entire message in an [italic] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ItalicsFulltextChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        return InsertAroundMessage(message, new MarkupNode("italic", null, null));
    }
}
