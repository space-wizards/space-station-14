using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

// CHAT-TODO: This could probably be used to do a more generic string insertion modifier for non-LoC strings too
/// <summary>
/// Inserts a localized string after a specific node. Useful for formatting certain messages, such as whispering.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class InsertLocChatModifier : ChatModifier
{
    // CHAT-TODO: Might be worth refactoring this into a base class with insertAFter/insertBefore derived types. Having it be a var might be considered ugly.
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
    public string LocString = default!;

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
