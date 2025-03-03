using Robust.Shared.Utility;

namespace Content.Shared.Chat.ContentMarkupTags;

public abstract class ContentMarkupTagBase
{
    /// <summary>
    /// The string used as the tags name when writing rich text
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Processes another markup node with this content tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessMarkupNode(MarkupNode node, int randomSeed)
    {
        return [];
    }

    /// <summary>
    /// Processes a text node with this content tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessTextNode(MarkupNode node, int randomSeed)
    {
        return [];
    }

    /// <summary>
    /// Returns a list of nodes replacing the opening markup node for this tag.
    /// Note: If you include a non-text node in the return list that is not closed, you MUST include a closing tag in CloserProcessing.
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        return [];
    }

    /// <summary>
    /// Returns a list of nodes replacing the closing markup node for this tag.
    /// Note: Any non-text node in the return list MUST include a closing node as well!
    /// </summary>
    public virtual IReadOnlyList<MarkupNode> ProcessCloser(MarkupNode node, int randomSeed)
    {
        return [];
    }
}
