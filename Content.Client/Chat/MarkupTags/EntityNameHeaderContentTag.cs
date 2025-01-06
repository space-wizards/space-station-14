using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTag : IContentMarkupTag
{
    public string Name => "EntityNameHeader";

    public List<MarkupNode>? OpenerProcessing(MarkupNode node)
    {

        var list = new List<MarkupNode>();
        var name = node.Value.StringValue;

        if (name == null)
            return null;

        list.Add(new MarkupNode(name));

        return list;
    }
}
