using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    public List<MessengerContact> Contacts;
    public string? ActiveContactName;
    public List<MessengerMessageData> ActiveMessages;
    public string CurrentOwnerName;

    public MessengerUiState(
        List<MessengerContact> contacts,
        string? activeContactName,
        List<MessengerMessageData> activeMessages,
        string currentOwnerName)
    {
        Contacts = contacts;
        ActiveContactName = activeContactName;
        ActiveMessages = activeMessages;
        CurrentOwnerName = currentOwnerName;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerContact
{
    public string Name;
    public string? JobTitle;
    public bool HasUnread;

    public MessengerContact(string name, string? jobTitle, bool hasUnread)
    {
        Name = name;
        JobTitle = jobTitle;
        HasUnread = hasUnread;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerMessageData
{
    public string Sender;
    public string Content;
    public TimeSpan Timestamp;

    public MessengerMessageData(string sender, string content, TimeSpan timestamp)
    {
        Sender = sender;
        Content = content;
        Timestamp = timestamp;
    }
}