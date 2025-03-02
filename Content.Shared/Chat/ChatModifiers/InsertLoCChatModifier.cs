using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Inserts an LoC string after a specific node. Useful for formatting certain messages, such as whispering.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class InsertLoCChatModifier : ChatModifier
{
    /// <summary>
    /// If false, the string will be inserted before the node.
    /// </summary>
    [DataField]
    public bool AfterNode = true;

    /// <summary>
    /// The node that the string should be inserted next to.
    /// </summary>
    [DataField]
    public string? TargetNode = null;

    /// <summary>
    /// The string that should be inserted.
    /// </summary>
    [DataField]
    public string LocString = "";

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if (TargetNode == null)
            return message;

        var str = Loc.GetString(LocString);
        if (AfterNode)
            message.InsertAfterTag(new MarkupNode(str), TargetNode);
        else
            message.InsertBeforeTag(new MarkupNode(str), TargetNode);
        return message;
    }
}
