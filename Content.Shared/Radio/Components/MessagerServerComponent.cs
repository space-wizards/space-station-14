using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MessagerServerComponent : Component
{
    public List<MessagerUser> Users = new();

    public List<MessagerMessage> Messages = new();
}

[Serializable, NetSerializable]
public sealed partial class MessagerUser
{
    public int Id;
    public string Name;

    public MessagerUser(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed partial class MessagerMessage
{
    public int Id;
    public int SenderId;
    public int ReceiverId;
    public string Content;
    public DateTime Timestamp;

    public MessagerMessage(int id, int senderId, int receiverId, string content, DateTime timestamp)
    {
        Id = id;
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
        Timestamp = timestamp;
    }
}