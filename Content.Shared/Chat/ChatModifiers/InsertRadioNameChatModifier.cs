using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Decals;
using Content.Shared.Radio;
using Robust.Shared.Configuration;
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
        IoCManager.InjectDependencies(this);
        if (channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel) &&
            _prototypeManager.TryIndex((string)radioChannel, out RadioChannelPrototype? radioPrototype))
        {
            if (TargetNode != null)
            {
                var str = "[" + Loc.GetString(radioPrototype.LocalizedName) + "] ";

                if (AfterNode)
                {
                    message.InsertAfterTag(new MarkupNode(str), TargetNode);
                    return;
                }

                message.InsertBeforeTag(new MarkupNode(str), TargetNode);
            }
        }
    }
}
