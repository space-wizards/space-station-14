using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Repository;

[Serializable, NetSerializable]
public sealed class MessagePatchedEvent : EntityEventArgs
{
    public uint MessageId;
    public string NewMessage;

    public MessagePatchedEvent(uint id, string newMessage)
    {
        MessageId = id;
        NewMessage = newMessage;
    }
}

[Serializable, NetSerializable]
public sealed class MessageDeletedEvent : EntityEventArgs
{
    public uint MessageId;

    public MessageDeletedEvent(uint id)
    {
        MessageId = id;
    }
}

// It is the only way to be sure.
[Serializable, NetSerializable]
public sealed class MessagesNukedEvent : EntityEventArgs
{
    public uint[] messageIds;

    public MessagesNukedEvent(IEnumerable<uint> set)
    {
        messageIds = set.ToArray();
    }
}

