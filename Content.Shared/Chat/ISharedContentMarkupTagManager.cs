using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public interface ISharedContentMarkupTagManager
{
    IReadOnlyDictionary<string, IContentMarkupTag> ContentMarkupTagTypes { get; }

    public IContentMarkupTag? GetMarkupTag(string name)
    {
        return ContentMarkupTagTypes.GetValueOrDefault(name);
    }

    /// <summary>
    /// Try to get the markup tag with the provided name.
    /// </summary>
    /// <param name="name">The name of the markup tag to check for.</param>
    /// <returns></returns>
    public bool TryGetContentMarkupTag(string name, [NotNullWhen(true)] out IContentMarkupTag? tag)
    {
        if (ContentMarkupTagTypes.TryGetValue(name, out var markupTag))
        {
            tag = markupTag;
            return true;
        }

        tag = null;
        return false;
    }

    /// <summary>
    /// Processes the message and applies the ContentMarkupTags.
    /// </summary>
    /// <param name="message">The input message.</param>
    /// <param name="tagStack">If used iteratively, tagStack includes existing tags acting on the message.</param>
    /// <returns></returns>
    public FormattedMessage ProcessMessage(FormattedMessage message, Stack<IContentMarkupTag>? tagStack = null)
    {
        var consumedNodes = tagStack ?? new Stack<IContentMarkupTag>();
        var returnMessage = new FormattedMessage();
        var randomSeed = message.Count; // CHAT-TODO: Replace with message uuid.

        var nodeEnumerator = message.Nodes.ToList();

        var i = 0;
        while (i < nodeEnumerator.Count)
        {
            var node = nodeEnumerator[i];

            // Iteratively go through all nodes that have been consumed and are acting on the message.
            if (consumedNodes.Count > 0)
            {
                var consumedNode = consumedNodes.First();

                var consumedNodeResult = node.Name != null
                    ? consumedNode.ProcessMarkupNode(node, randomSeed)
                    : consumedNode.ProcessTextNode(node, randomSeed);

                if (consumedNodeResult != null)
                {
                    var iteratedMessage = ProcessMessage(FormattedMessage.FromMarkupOrThrow(string.Join("", consumedNodeResult)), new Stack<IContentMarkupTag>(consumedNodes.Skip(1)));
                    returnMessage.AddMessage(iteratedMessage);
                    nodeEnumerator.InsertRange(i, iteratedMessage.Nodes);
                    i += iteratedMessage.Nodes.Count + 1;
                    continue;
                }
            }

            // Handles extracting the ContentMarkupTags and applies any processes that those tags have set.
            if (node.Name != null && TryGetContentMarkupTag(node.Name, out var tag))
            {
                if (!node.Closing)
                {
                    var openerNode = tag.ProcessOpeningTag(node, randomSeed);
                    if (openerNode != null)
                    {
                        nodeEnumerator.InsertRange(i, openerNode);
                        i += openerNode.Count;
                        returnMessage.AddMessage(FormattedMessage.FromMarkupOrThrow(string.Join("", openerNode)));
                    }

                    consumedNodes.Push(tag);
                }
                else
                {
                    var closerNode = tag.ProcessCloser(node, randomSeed);
                    if (closerNode != null)
                    {
                        nodeEnumerator.InsertRange(i, closerNode);
                        i += closerNode.Count;
                        returnMessage.AddMessage(FormattedMessage.FromMarkupOrThrow(string.Join("", closerNode)));
                    }

                    consumedNodes.Pop();
                }
            }
            else
            {
                if (!node.Closing)
                {
                    returnMessage.PushTag(node);
                }
                else
                {
                    returnMessage.Pop();
                }
            }

            i++;
        }

        return returnMessage;
    }
}
