using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public sealed class ContentMarkupTagFactory(
    IReadOnlyDictionary<string, Func<MarkupNode, ContentMarkupTagProcessorBase>> contentMarkupTagTypes
)
{
    public bool TryGetProcessor(MarkupNode node, [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor)
    {
        processor = null;
        if (node.Name == null || node.Closing)
        {
            return false;
        }

        if (contentMarkupTagTypes.TryGetValue(node.Name, out var factoryMethod))
        {
            processor = factoryMethod(node);
            return true;
        }

        return false;
    }
}
