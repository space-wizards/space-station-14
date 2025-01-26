using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Adds a space in front of the [MainMessage] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpaceMainMessageChatModifier : ChatModifier
{
    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        message.InsertBeforeTag(new MarkupNode(" "), "MainMessage");
    }
}
