using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Chat.Testing;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public sealed class ContentMarkupTagManager
{
    private static readonly Dictionary<string, IContentMarkupTag> _contentMarkupTagTypes = new IContentMarkupTag[] {
        new TestContentTag(),
    }.ToDictionary(x => x.Name, x => x);

    public IContentMarkupTag? GetMarkupTag(string name)
    {
        return _contentMarkupTagTypes.GetValueOrDefault(name);
    }

    public static bool TryGetContentMarkupTag(string name, Type[]? tagsAllowed, [NotNullWhen(true)] out IContentMarkupTag? tag)
    {
        if (_contentMarkupTagTypes.TryGetValue(name, out var markupTag)
            // Using a whitelist prevents new tags from sneaking in.
            && (tagsAllowed == null || Array.IndexOf(tagsAllowed, markupTag.GetType()) != -1))
        {
            tag = markupTag;
            return true;
        }

        tag = null;
        return false;
    }

    public static FormattedMessage ProcessMessage(FormattedMessage message, Stack<IContentMarkupTag>? tagStack = null)
    {
        var consumedNodes = tagStack ?? new Stack<IContentMarkupTag>();
        var returnMessage = new FormattedMessage();

        var nodeEnumerator = message.Nodes.ToList();

        var i = 0;
        while (i < nodeEnumerator.Count)
        {
            var node = nodeEnumerator[i];

            Logger.Debug("nodename: " + (node.Name ?? "TEXTNODE"));
            if (consumedNodes.Count > 0)
            {
                var consumedNode = consumedNodes.First();
                //foreach (var consumedNode in consumedNodes)
                //{
                var consumedNodeResult = node.Name != null
                    ? consumedNode.MarkupNodeProcessing(node)
                    : consumedNode.TextNodeProcessing(node);
                if (consumedNodeResult != null)
                {
                    var testvar = string.Join("", consumedNodeResult);
                    Logger.Debug("testvar " + testvar);
                    var secondtestvar = ProcessMessage(FormattedMessage.FromMarkupOrThrow(testvar), new Stack<IContentMarkupTag>(consumedNodes.Skip(1)));
                    Logger.Debug("secondtestvar " + secondtestvar.ToMarkup());
                    returnMessage.AddMessage(secondtestvar);
                    nodeEnumerator.InsertRange(i, secondtestvar.Nodes);
                    Logger.Debug("nodecount: " + secondtestvar.Nodes.Count);
                    i += secondtestvar.Nodes.Count + 1;
                    continue;
                }
            }
            //}

            if (node.Name != null && TryGetContentMarkupTag(node.Name, null, out var tag))
            {
                Logger.Debug("- node: " + node.Name);
                if (!node.Closing)
                {
                    var openerNode = tag.OpenerProcessing(node);
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
                    var closerNode = tag.CloserProcessing(node);
                    if (closerNode != null)
                    {
                        nodeEnumerator.InsertRange(i, closerNode);
                        i += closerNode.Count;
                        Logger.Debug(string.Join("", closerNode));
                        returnMessage.AddMessage(FormattedMessage.FromMarkupOrThrow(string.Join("", closerNode)));
                    }

                    consumedNodes.Pop();
                }
            }
            else
            {
                Logger.Debug("- textnode: " + (node.Value.StringValue ?? "not contenttag/no value"));
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
