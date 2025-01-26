using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
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

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        if (TargetNode != null)
            message.InsertOutsideTag(new MarkupNode("bold", null, null), TargetNode);
    }
}
