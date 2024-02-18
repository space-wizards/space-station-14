using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessagesUiAction Action;
    public readonly MessagesMessageData? MessageData;
    public readonly string? Chat;

    public MessagesUiMessageEvent(MessagesUiAction action, MessagesMessageData messageData, string? chat="")
    {
        Action = action;
        MessageData = messageData;
        Chat = chat;
    }
}

[Serializable, NetSerializable]
public enum MessagesUiAction
{
    Send,
    ChangeChat
}

[Serializable, NetSerializable]
public partial struct MessagesMessageData
{
    public string SenderId;
    public string ReceiverId;
    public string Content;
    public double Time;
}
