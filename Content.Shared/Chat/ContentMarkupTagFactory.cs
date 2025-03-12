using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

/// <summary>
/// Factory for providing content markup tag processors.
/// </summary>
/// <remarks>
/// As each processor handles tags based on opening tag, and nodes of same type can be nested inside each other,
/// we could require several instances of same processor but initialized with different values.
/// </remarks>
/// <param name="tryCreateMethodsByMarkupTagName">
/// Pre-set list of factory-methods. If factory method returns false - node does not need processor to be handled.
/// </param>
public sealed class ContentMarkupTagFactory(
    IReadOnlyDictionary<string, ContentMarkupTagProcessorProvider> tryCreateMethodsByMarkupTagName
)
{
    /// <summary>
    /// Attempts to create markup node processor for node with passed name.
    /// </summary>
    /// <param name="node">MarkupNode that starts sequence of nodes that processor should try to handle.</param>
    /// <param name="context">Context, in which message is process.</param>
    /// <param name="processor">Created processor or null.</param>
    /// <returns>False if node name is empty, or if there is no pre-set factory method for passed node.</returns>
    public bool TryGetProcessor(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        processor = null;
        if (node.Name == null || node.Closing)
        {
            return false;
        }

        if (tryCreateMethodsByMarkupTagName.TryGetValue(node.Name, out var tryCreateMethod)
            && tryCreateMethod(node, context, out processor))
        {

            return true;
        }

        return false;
    }
}

/// <summary>
/// Func signature for creating ContentMarkupTagProcessorBase out of <see cref="MarkupNode"/> and current processing context.
/// </summary>
/// <param name="node">MarkupNode that starts sequence of nodes that processor should try to handle.</param>
/// <param name="context">Context, in which message is process.</param>
public delegate bool ContentMarkupTagProcessorProvider(
    MarkupNode node,
    ChatMessageContext context,
    [NotNullWhen(true)]out ContentMarkupTagProcessorBase? processor
);
