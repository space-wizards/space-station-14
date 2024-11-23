using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.Testing;

public sealed class TestContentTag : IContentMarkupTag
{
    public string Name => "TestMarkup";

    public List<MarkupNode>? TextNodeProcessing(MarkupNode node)
    {
        var returnNodes = new List<MarkupNode>();
        returnNodes.Add(new MarkupNode("bold", null, null));
        returnNodes.Add(node);

        returnNodes.Add(new MarkupNode("bold", null, null, true));
        return returnNodes;
    }

    public List<MarkupNode>? OpenerProcessing(MarkupNode node)
    {
        return new List<MarkupNode>() { new MarkupNode("color", new MarkupParameter(Color.Aqua), null) };
    }

    public List<MarkupNode>? CloserProcessing(MarkupNode node)
    {
        return new List<MarkupNode>() { new MarkupNode("color", null, null, true) };
    }
}
