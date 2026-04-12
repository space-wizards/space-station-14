using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessengerUiAction Action;
    public readonly string? RecipientName;
    public readonly string? MessageContent;

    public MessengerUiMessageEvent(MessengerUiAction action, string? recipientName = null, string? messageContent = null)
    {
        Action = action;
        RecipientName = recipientName;
        MessageContent = messageContent;
    }
}

[Serializable, NetSerializable]
public enum MessengerUiAction
{
    OpenChat,
    Send,
    Back
}