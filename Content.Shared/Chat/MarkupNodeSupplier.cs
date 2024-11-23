using System.Linq;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;


/// <summary>
/// Used to add markup nodes to FormattedMessages, preparing them to later be processed by MarkupTagManager.
/// </summary>
[Serializable]
[DataDefinition]
[Virtual]
public partial class MarkupNodeSupplier
{
    /// <summary>
    /// Returns a FormattedMessage after it has been processed by the node supplier.
    /// </summary>
    /// <param name="message">The message to be processed.</param>
    /// <returns></returns>
    public virtual FormattedMessage ProcessNodeSupplier(FormattedMessage message)
    {
        return message;
    }

    /// <summary>
    /// Helper function that inserts a node around an existing node.
    /// </summary>
    protected FormattedMessage InsertOutsideTag(FormattedMessage message, MarkupNode newNode, string tagText)
    {
        var returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;
            if (node.Name == tagText)
            {
                if (!node.Closing)
                {
                    returnMessage.PushTag(newNode, false);
                }
                else
                {
                    returnMessage.Pop();
                }
            }

            if (!node.Closing)
                returnMessage.PushTag(node);
            else
                returnMessage.Pop();
        }

        nodeEnumerator.Dispose();
        return returnMessage;
    }

    /// <summary>
    /// Helper function that inserts a node before another node.
    /// </summary>
    protected FormattedMessage InsertBeforeTag(FormattedMessage message, MarkupNode newNode, string tagText)
    {
        var returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;
            if (node.Name == tagText)
            {
                if (!node.Closing)
                {
                    returnMessage.PushTag(newNode, true);
                }
            }

            if (!node.Closing)
                returnMessage.PushTag(node);
            else
                returnMessage.Pop();
        }

        nodeEnumerator.Dispose();
        return returnMessage;
    }

    /// <summary>
    /// Helper function that inserts a node inside of another node.
    /// </summary>
    protected FormattedMessage InsertInsideTag(FormattedMessage message, MarkupNode newNode, string tagText)
    {
        var returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;

            if (!node.Closing)
                returnMessage.PushTag(node);
            else
                returnMessage.Pop();

            if (node.Name == tagText)
            {
                if (!node.Closing)
                {
                    returnMessage.PushTag(newNode, false);
                }
                else
                {
                    returnMessage.Pop();
                }
            }
        }

        nodeEnumerator.Dispose();
        return returnMessage;
    }

    /// <summary>
    /// Helper function that inserts a node after an existing tag.
    /// </summary>
    protected FormattedMessage InsertAfterTag(FormattedMessage message, MarkupNode newNode, string tagText)
    {
        var returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;

            if (!node.Closing)
                returnMessage.PushTag(node);
            else
                returnMessage.Pop();

            if (node.Name == tagText)
            {
                if (node.Closing)
                {
                    returnMessage.PushTag(newNode, true);
                }
            }
        }

        nodeEnumerator.Dispose();
        return returnMessage;
    }

    /// <summary>
    /// Helper function that inserts a node around a string.
    /// </summary>
    protected FormattedMessage InsertAroundString(FormattedMessage message, MarkupNode newNode, string stringText)
    {
        var returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;

            if (node.Name == null && node.Value.StringValue != null)
            {
                var stringNode = node.Value.StringValue;
                var stringLocation = stringNode.IndexOf(stringText, StringComparison.Ordinal);
                while (stringLocation != -1)
                {
                    var beforeText = stringNode.Substring(0, stringLocation);
                    if (beforeText != "")
                        returnMessage.AddText(beforeText);
                    returnMessage.PushTag(newNode, false);
                    returnMessage.AddText(stringText);
                    returnMessage.Pop();

                    stringNode = stringNode.Substring(stringLocation + stringText.Length);
                    stringLocation = stringNode.IndexOf(stringText, StringComparison.Ordinal);
                }

                if (stringNode != "")
                {
                    returnMessage.AddText(stringNode);
                }
            }
            else
            {
                if (!node.Closing)
                    returnMessage.PushTag(node);
                else
                    returnMessage.Pop();
            }
        }

        nodeEnumerator.Dispose();
        return returnMessage;
    }
}
