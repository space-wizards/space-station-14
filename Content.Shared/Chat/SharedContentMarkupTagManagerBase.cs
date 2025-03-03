using System.Linq;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract class SharedContentMarkupTagManagerBase
{
    /// <summary>
    /// Map of initialized markup tags by their name.
    /// Any new ContentMarkupTag must be included in the Content.Client/Server's implementation of this dictionary.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, ContentMarkupTagBase> ContentMarkupTagTypes { get; }

    /// <summary>
    /// Processes the message and applies the ContentMarkupTags.
    /// </summary>
    /// <param name="nodes">The input message.</param>
    /// <param name="tagStack">If used iteratively, tagStack includes existing tags acting on the message.</param>
    /// <returns>Message that is a result of application of all MarkupTags.</returns>
    public IReadOnlyCollection<MarkupNode> ProcessMessage(IReadOnlyCollection<MarkupNode> nodes, Stack<ContentMarkupTagBase>? tagStack = null)
    {
        var consumedNodes = tagStack ?? new Stack<ContentMarkupTagBase>();
        var result = new List<MarkupNode>();
        var randomSeed = nodes.Count; // CHAT-TODO: Replace with message uuid.

        // CHAT-TODO: This code is funky and fildrance should be poked about it later to figure out how to make it optimized.
        var messageNodes = nodes.ToList();

        var stack = new Stack<MarkupNode>();

        var i = 0;
        while (i < messageNodes.Count)
        {
            var node = messageNodes[i];

            // Iteratively go through all nodes that have been consumed and are acting on the message.
            if (consumedNodes.Count > 0)
            {
                var consumedNode = consumedNodes.First();

                var consumedNodeResult = node.Name != null
                    ? consumedNode.ProcessMarkupNode(node, randomSeed)
                    : consumedNode.ProcessTextNode(node, randomSeed);

                if (consumedNodeResult.Count > 0)
                {
                    var processed = ProcessMessage(consumedNodeResult, new(consumedNodes.Skip(1)));
                    result.AddRange(processed);
                    messageNodes.InsertRange(i, processed);
                    i += processed.Count + 1;
                    continue;
                }
            }

            // Handles extracting the ContentMarkupTags and applies any processes that those tags have set.
            if (node.Name != null && ContentMarkupTagTypes.TryGetValue(node.Name, out var tag))
            {
                if (!node.Closing)
                {
                    var openerNode = tag.ProcessOpeningTag(node, randomSeed);
                    if (openerNode.Count > 0)
                    {
                        messageNodes.InsertRange(i, openerNode);
                        i += openerNode.Count;
                        result.AddRange(openerNode);
                    }

                    consumedNodes.Push(tag);
                }
                else
                {
                    var closerNode = tag.ProcessCloser(node, randomSeed);
                    if (closerNode.Count > 0)
                    {
                        messageNodes.InsertRange(i, closerNode);
                        i += closerNode.Count;
                        result.AddRange(closerNode);
                    }

                    consumedNodes.Pop();
                }
            }
            else
            {
                if (!node.Closing)
                {
                    result.Add(node);
                    stack.Push(node);
                }
                else
                {
                    var lastOpened = stack.Pop();
                    result.Add(new MarkupNode(lastOpened.Name, null, null, true));
                }
            }

            i++;
        }

        return result;
    }
}
