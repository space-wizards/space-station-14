using Robust.Shared.Utility;

namespace Content.Shared.Chat;

/// <summary>
/// Used to add markup nodes to FormattedMessages, preparing them to later be processed by MarkupTagManager.
/// </summary>
[Serializable]
[DataDefinition]
[Virtual]
public abstract partial class ChatModifier
{
    /// <summary>
    /// Returns a FormattedMessage after it has been processed by the node supplier.
    /// </summary>
    /// <param name="message">The message to be processed.</param>
    /// <param name="chatMessageContext">Any parameters that can be handled by the suppliers.</param>
    public abstract FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext);
}
