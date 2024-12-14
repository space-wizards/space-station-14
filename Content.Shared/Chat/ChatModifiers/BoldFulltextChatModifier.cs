using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the entire message in an [italic] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BoldFulltextChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        return InsertAroundMessage(message, new MarkupNode("bold", null, null));
    }
}
