using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Inserts an LoC string after a specific node. Useful for formatting certain messages, such as whispering.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class InsertRadioNameChatModifier : ChatModifier
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// The node that the string should be inserted next to.
    /// </summary>
    [DataField]
    public string? TargetNode = null;

    /// <summary>
    /// If false, the string will be inserted before the node.
    /// </summary>
    [DataField]
    public bool AfterNode = true;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        if(!chatMessageContext.TryGet<string>(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return message;

        IoCManager.InjectDependencies(this);

        if (!_prototypeManager.TryIndex(radioChannel, out RadioChannelPrototype? radioPrototype))
            return message;

        if (TargetNode == null)
            return message;

        var str = "[" + Loc.GetString(radioPrototype.LocalizedName) + "] ";

        if (AfterNode)
        {
            message.InsertAfterTag(new MarkupNode(str), TargetNode);
            return message;
        }

        message.InsertBeforeTag(new MarkupNode(str), TargetNode);
        return message;
    }
}
