using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MessagerServerComponent : Component
{
    public Dictionary<int, MessagerUser> Users = new();
    public bool InitialBroadcastDone = false;

    public List<MessagerMessage> Messages = new();
}

[Serializable, NetSerializable]
public sealed partial class MessagerUser
{
    public int Id;
    public string Name;
    public string JobIconId;
    public string JobTitle;
    public Dictionary<int, int> UnreadCounts = new();

    public MessagerUser(int id, string name, string jobIconId, string jobTitle)
    {
        Id = id;
        Name = name;
        JobIconId = jobIconId;
        JobTitle = jobTitle;
    }
}

[Serializable, NetSerializable]
public sealed partial class MessagerMessage
{
    public int Id;
    public int SenderId;
    public int ReceiverId;
    public string Content;
    public TimeSpan Timestamp;

    public MessagerMessage(int id, int senderId, int receiverId, string content, TimeSpan timestamp)
    {
        Id = id;
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
        Timestamp = timestamp;
    }
}
