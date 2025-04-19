using System.Linq;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public sealed class SharedContentMarkupTagManager(ContentMarkupTagFactory factory)
{
    /// <summary>
    /// Processes the message and applies the message's ContentMarkupTags.
    /// </summary>
    /// <param name="nodes">The input message.</param>
    /// <param name="context">Context in which message is being processed.</param>
    /// <returns>Message that is a result of application of all MarkupTags.</returns>
    public IReadOnlyCollection<MarkupNode> ProcessMessage(
        IReadOnlyCollection<MarkupNode> nodes,
        ChatMessageContext context
    )
    {
        return ProcessRecursively(factory, nodes, context, new List<ContentMarkupTagProcessorBase>());
    }

    /// <summary>
    /// Builds and recursively processes a list of input nodes and processors.
    /// Since a processor may return additional nodes, any markup tag surrounding it must also be applied to those new nodes,
    /// which is why we do it recursively.
    /// </summary>
    private static IReadOnlyCollection<MarkupNode> ProcessRecursively(
        ContentMarkupTagFactory factory,
        IReadOnlyCollection<MarkupNode> inputNodes,
        ChatMessageContext context,
        List<ContentMarkupTagProcessorBase> activeProcessors)
    {
        var nodeList = inputNodes.ToList();
        var result = new List<MarkupNode>();
        var i = 0;
        while (i < nodeList.Count)
        {
            var node = nodeList[i];

            // If the node is a ContentMarkupTagProcessor, we get it out now
            if (factory.TryGetProcessor(node, context, out var processor))
            {
                activeProcessors.Add(processor);
            }

            // If we have any active processors, start applying. If not, just add the node.
            if (activeProcessors.Count() > 0)
            {
                var processorIndex = activeProcessors.Count - 1;
                var currentProcessor = activeProcessors[processorIndex];

                if (!currentProcessor.Process(node, out var processedNodes))
                {
                    // If top level processor is saying it was closed (returns false) then we remove it from the list
                    activeProcessors.Remove(currentProcessor);
                }

                if (processedNodes.Count > 0)
                {
                    // Any processor under the top level one needs to act on the contents of the node; hence recursive processing.
                    // TODO: Processors that only care about replacing its own opening & closing nodes probably should not count for this check; makes for unnecessary recursions.
                    if (processorIndex >= 1)
                        result.AddRange(ProcessRecursively(factory, processedNodes, context, activeProcessors.GetRange(0, processorIndex)));
                    else
                        result.AddRange(processedNodes);
                }
            }
            else
            {
                result.Add(node);
            }

            i++;
        }

        return result;
    }
}
