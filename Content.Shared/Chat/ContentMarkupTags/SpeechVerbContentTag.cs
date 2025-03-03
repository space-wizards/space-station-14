using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ContentMarkupTags;

public sealed class SpeechVerbContentTag : ContentMarkupTagBase
{
    public override string Name => "SpeechVerb";

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        if (node.Value.TryGetString(out var speech) &&
            node.Attributes.TryGetValue("id", out var idKey) &&
            idKey.TryGetLong(out var id) &&
            IoCManager.Resolve<IPrototypeManager>().TryIndex(speech, out SpeechVerbPrototype? speechVerbPrototype))
        {
            return new List<MarkupNode> { new MarkupNode(" " + Loc.GetString(speechVerbPrototype.SpeechVerbStrings[(int)id]) + ",") };
        }

        return [];
    }
}
