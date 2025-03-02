using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the first instance of a [TargetTag] tag in a [bold] tag.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BoldTagChatModifier : ChatModifier
{
    [DataField]
    public string? TargetNode = null;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (TargetNode != null)
            message.InsertOutsideTag(new MarkupNode("bold", null, null), TargetNode);

        return message;
    }
}
