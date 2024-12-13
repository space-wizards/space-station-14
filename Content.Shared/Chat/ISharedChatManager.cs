using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public interface ISharedChatManager
{
    void Initialize();

    /// <summary>
    /// Helper function that tries to find the first instance of a tag and returns a FormattedMessage containing the nodes inside.
    /// </summary>
    /// <returns></returns>
    public bool TryGetMessageInsideTag(FormattedMessage message, [NotNullWhen(true)] out FormattedMessage? returnMessage, string tagText)
    {
        returnMessage = new FormattedMessage();
        var nodeEnumerator = message.GetEnumerator();
        var nodeFound = false;

        while (nodeEnumerator.MoveNext())
        {
            var node = nodeEnumerator.Current;

            if (node.Name == tagText)
            {
                if (!node.Closing)
                {
                    nodeFound = true;
                }
                else
                {
                    nodeEnumerator.Dispose();
                    return true;
                }
            }
            else if (nodeFound)
            {
                if (!node.Closing)
                    returnMessage.PushTag(node);
                else
                    returnMessage.Pop();
            }
        }

        nodeEnumerator.Dispose();
        return false;
    }
}
