using System.Diagnostics.CodeAnalysis;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ContentMarkupTags;

public sealed class SpeechVerbContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "SpeechVerb";

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node)
    {
        if (node.Value.TryGetString(out var speech) &&
            node.Attributes.TryGetValue("id", out var idKey) &&
            idKey.TryGetLong(out var id) &&
            IoCManager.Resolve<IPrototypeManager>().TryIndex(speech, out SpeechVerbPrototype? speechVerbPrototype))
        {
            return new List<MarkupNode>
                { new MarkupNode(" " + Loc.GetString(speechVerbPrototype.SpeechVerbStrings[(int)id]) + ",") };
        }

        return [];
    }

    public static bool TryCreate(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        processor = new SpeechVerbContentTagProcessor();
        return true;
    }
}
