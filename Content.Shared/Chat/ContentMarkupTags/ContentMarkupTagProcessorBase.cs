using Robust.Shared.Utility;

namespace Content.Shared.Chat.ContentMarkupTags;

public abstract class ContentMarkupTagProcessorBase
{
    /// <summary>
    /// The string used as the tags name when writing rich text
    /// </summary>
    public abstract string Name { get; }

    public bool Process(MarkupNode node, bool isTopLevelProcessor, out IReadOnlyList<MarkupNode> markupNodes)
    {
        if (node.Name == null)
        {
            markupNodes = ProcessTextNode(node);
            return true;
        }

        if (node.Name == Name && node.Closing && isTopLevelProcessor)
        {
            markupNodes = ProcessCloser(node);
            return false;
        }

        if (node.Name == Name && !node.Closing && isTopLevelProcessor)
        {
            markupNodes = ProcessOpeningTag(node);
            return true;
        }

        markupNodes = ProcessMarkupNode(node);

        return true;
    }

    /// <summary>
    /// Processes another markup node with this content tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessMarkupNode(MarkupNode node)
    {
        return [];
    }

    /// <summary>
    /// Processes a text node with this content tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessTextNode(MarkupNode node)
    {
        return [];
    }

    /// <summary>
    /// Returns a list of nodes replacing the opening markup node for this tag.
    /// Note: If you include a non-text node in the return list that is not closed, you MUST include a closing tag in CloserProcessing.
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node)
    {
        return [];
    }

    /// <summary>
    /// Returns a list of nodes replacing the closing markup node for this tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessCloser(MarkupNode node)
    {
        return [];
    }

}
