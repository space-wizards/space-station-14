using Robust.Shared.Serialization;
using Content.Shared.CartridgeLoader;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagerCartridgeUiState : BoundUserInterfaceState
{
    public MessagerStatus Status;
    public Dictionary<int, MessagerUserEntry> Users;
    public List<MessagerMessageEntry> Messages;
    public bool HasNewMessage;

    public MessagerCartridgeUiState(MessagerStatus status, Dictionary<int, MessagerUserEntry> users, List<MessagerMessageEntry>? messages = null, bool hasNewMessage = false)
    {
        Status = status;
        Users = users;
        Messages = messages ?? new List<MessagerMessageEntry>();
        HasNewMessage = hasNewMessage;
    }
}

/// <summary>
/// User list client
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class MessagerUserEntry
{
    public int Id;
    public string Name;
    public string JobIconId;
    public string JobTitle;
    public int UnreadCount;

    public MessagerUserEntry(int id, string name, string jobIconId, string jobTitle, int unreadCount = 0)
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
public sealed partial class MessagerMessageEntry
{
    public int Id;
    public string SenderName;
    public string Content;
    public TimeSpan Timestamp;
    public bool IsIncoming;
    public int SenderId;
    public int ReceiverId;

    public MessagerMessageEntry(int id, string content, TimeSpan timestamp, int senderId, int receiverId)
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
///
/// </summary>
[Serializable, NetSerializable]
public sealed class MessagerSendMessageEvent : CartridgeMessageEvent
{
    public int ReceiverId;
    public string Content;

    public MessagerSendMessageEvent(int receiverId, string content)
    {
        ReceiverId = receiverId;
        Content = content;
    }
}


/// <summary>
/// UI update request
/// </summary>
[Serializable, NetSerializable]
public sealed class MessagerRequestMessagesEvent : CartridgeMessageEvent
{
    public int UserId;

    public MessagerRequestMessagesEvent(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Server connection status
/// </summary>
[Serializable, NetSerializable]
public enum MessagerStatus : byte
{
    Connecting,
    Connected,
    ConnectionLost
};
