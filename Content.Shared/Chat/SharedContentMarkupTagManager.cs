using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public sealed class SharedContentMarkupTagManager(ContentMarkupTagFactory factory)
{
    /// <summary>
    /// Processes the message and applies the ContentMarkupTags.
    /// </summary>
    /// <param name="nodes">The input message.</param>
    /// <param name="context">Context in which message is being processed.</param>
    /// <returns>Message that is a result of application of all MarkupTags.</returns>
    public IReadOnlyCollection<MarkupNode> ProcessMessage(
        IReadOnlyCollection<MarkupNode> nodes,
        ChatMessageContext context
    )
    {
        var result = new List<MarkupNode>();

        var activeProcessors = new List<ContentMarkupTagProcessorBase>();
        foreach (var node in nodes)
        {
            if (factory.TryGetProcessor(node, context, out var processor))
            {
                activeProcessors.Add(processor);
            }

            var processedIntoResult = ExecuteProcessors(activeProcessors, node, result);

            if (!processedIntoResult)
                result.Add(node);
        }

        return result;
    }

    private static bool ExecuteProcessors(
        List<ContentMarkupTagProcessorBase> activeProcessors,
        MarkupNode node,
        List<MarkupNode> result
    )
    {
        var isTopLevelProcessor = true;
        var processedClosing = false;
        var processedIntoResult = false;

        // iterate starting from last added
        for (var processorIndex = activeProcessors.Count - 1; processorIndex >= 0; processorIndex--)
        {
            var currentProcessor = activeProcessors[processorIndex];
                
            if (!currentProcessor.Process(node, isTopLevelProcessor, out var processedNodes) && !processedClosing)
            {
                // if top level processor is saying it was closed (returns false) then we remove it and mark closing as handled
                activeProcessors.Remove(currentProcessor);
                processedClosing = true;
            }

            if (processedNodes.Count > 0)
            {
                result.AddRange(processedNodes);
                processedIntoResult = true;
            }

            isTopLevelProcessor = false;
        }

        return processedIntoResult;
    }
}
