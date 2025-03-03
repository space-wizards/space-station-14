using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTag : ContentMarkupTagBase
{
    public override string Name => "EntityNameHeader";

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        var name = node.Value.StringValue;
        if (name == null)
            return [];

        return new List<MarkupNode> { new MarkupNode(name) };
    }
}
