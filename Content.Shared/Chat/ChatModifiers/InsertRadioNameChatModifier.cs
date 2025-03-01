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

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        if(!channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return;

        IoCManager.InjectDependencies(this);
        if (!_prototypeManager.TryIndex((string)radioChannel, out RadioChannelPrototype? radioPrototype))
            return;

        if (TargetNode == null)
            return;

        var str = "[" + Loc.GetString(radioPrototype.LocalizedName) + "] ";

        if (AfterNode)
        {
            message.InsertAfterTag(new MarkupNode(str), TargetNode);
            return;
        }

        message.InsertBeforeTag(new MarkupNode(str), TargetNode);
    }
}
