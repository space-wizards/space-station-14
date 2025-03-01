using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class SessionNameHeaderContentTag : IContentMarkupTag
{
    public string Name => "SessionNameHeader";

    public List<MarkupNode>? ProcessOpeningTag(MarkupNode node)
    {

        var name = node.Value.StringValue;
        if (name == null)
            return null;

        return [new MarkupNode(name)];
    }
}
