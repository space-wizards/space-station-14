using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTag : IContentMarkupTag
{
    public string Name => "EntityNameHeader";

    public List<MarkupNode>? ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        var name = node.Value.StringValue;
        if (name == null)
            return null;

        return new List<MarkupNode>() { new MarkupNode(name) };
    }
}
