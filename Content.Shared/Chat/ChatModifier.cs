using System.Linq;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;


/// <summary>
/// Used to add markup nodes to FormattedMessages, preparing them to later be processed by MarkupTagManager.
/// </summary>
[Serializable]
[DataDefinition]
[Virtual]
public partial class ChatModifier
{
    /// <summary>
    /// Returns a FormattedMessage after it has been processed by the node supplier.
    /// </summary>
    /// <param name="message">The message to be processed.</param>
    /// <param name="channelParameters">Any parameters that can be handled by the suppliers.</param>
    /// <returns></returns>
    public virtual void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {

    }
}
