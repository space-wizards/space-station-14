using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MessengerServerComponent : Component
{
    public Dictionary<int, MessengerUser> Users = new();

    public List<MessengerMessage> Messages = new();
}

[Serializable, NetSerializable]
public sealed partial class MessengerUser
{
    public int Id;
    public string Name;
    public string JobIconId;
    public string JobTitle;
    public Dictionary<int, int> UnreadCounts = new();

    public MessengerUser(int id, string name, string jobIconId, string jobTitle)
    {
        Id = id;
        Name = name;
        JobIconId = jobIconId;
        JobTitle = jobTitle;
    }
}

[Serializable, NetSerializable]
public sealed partial class MessengerMessage
{
    public int Id;
    public int SenderId;
    public int ReceiverId;
    public string Content;
    public TimeSpan Timestamp;

    public MessengerMessage(int id, int senderId, int receiverId, string content, TimeSpan timestamp)
    {
        Id = id;
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
        Timestamp = timestamp;
    }
}
