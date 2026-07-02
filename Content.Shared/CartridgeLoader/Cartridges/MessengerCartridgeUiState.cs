using Robust.Shared.Serialization;
using Content.Shared.CartridgeLoader;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerCartridgeUiState : BoundUserInterfaceState
{
    public MessengerStatus Status;
    public Dictionary<int, MessengerUserEntry> Users;
    public List<MessengerMessageEntry> Messages;

    public MessengerCartridgeUiState(MessengerStatus status, Dictionary<int, MessengerUserEntry> users, List<MessengerMessageEntry>? messages = null)
    {
        Status = status;
        Users = users;
        Messages = messages ?? new List<MessengerMessageEntry>();
    }
}

/// <summary>
/// User list client
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class MessengerUserEntry
{
    public int Id;
    public string Name;
    public string JobIconId;
    public string JobTitle;
    public int UnreadCount;

    public MessengerUserEntry(int id, string name, string jobIconId, string jobTitle, int unreadCount = 0)
    {
        Id = id;
        Name = name;
        JobIconId = jobIconId;
        JobTitle = jobTitle;
        UnreadCount = unreadCount;
    }
}

/// <summary>
/// Message for UI display
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class MessengerMessageEntry
{
    public int Id;
    public string SenderName;
    public string Content;
    public TimeSpan Timestamp;
    public bool IsIncoming;
    public int SenderId;
    public int ReceiverId;

    public MessengerMessageEntry(int id, string content, TimeSpan timestamp, int senderId, int receiverId)
    {
        Id = id;
        SenderName = "";
        Content = content;
        Timestamp = timestamp;
        SenderId = senderId;
        ReceiverId = receiverId;
    }
}

/// <summary>
/// Send message event
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerSendMessageEvent : CartridgeMessageEvent
{
    public int ReceiverId;
    public string Content;

    public MessengerSendMessageEvent(int receiverId, string content)
    {
        ReceiverId = receiverId;
        Content = content;
    }
}


/// <summary>
/// UI update request
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerRequestMessagesEvent : CartridgeMessageEvent
{
    public int UserId;

    public MessengerRequestMessagesEvent(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Typing indicator event
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerTypingEvent : CartridgeMessageEvent
{
    public MessengerTypingEvent() { }
}

/// <summary>
/// Server connection status
/// </summary>
[Serializable, NetSerializable]
public enum MessengerStatus : byte
{
    Connected,
    ConnectionLost
};
