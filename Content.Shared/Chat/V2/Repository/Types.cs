using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Repository;

/// <summary>
/// The record associated with a specific chat event.
/// </summary>
public struct ChatRecord(string userName, IChatEvent storedEvent, string entityName, Vector2 location, string map)
{
    public string UserName = userName;
    public string EntityName = entityName;
    public Vector2 Location = location;
    public string Map = map;
    public IChatEvent StoredEvent = storedEvent;
}

/// <summary>
/// Notifies that a chat message has been created.
/// </summary>
/// <param name="ev"></param>
[Serializable, NetSerializable]
public sealed class MessageCreatedEvent(IChatEvent ev) : EntityEventArgs
{
    public IChatEvent Event = ev;
}

/// <summary>
/// Notifies that a chat message has been changed.
/// </summary>
/// <param name="id"></param>
/// <param name="newMessage"></param>
[Serializable, NetSerializable]
public sealed class MessagePatchedEvent(uint id, string newMessage) : EntityEventArgs
{
    public uint MessageId = id;
    public string NewMessage = newMessage;
}

/// <summary>
/// Notifies that a chat message has been deleted.
/// </summary>
/// <param name="id"></param>
[Serializable, NetSerializable]
public sealed class MessageDeletedEvent(uint id) : EntityEventArgs
{
    public uint MessageId = id;
}

/// <summary>
/// Notifies that a player's messages have been nuked.
/// </summary>
/// <param name="set"></param>
[Serializable, NetSerializable]
public sealed class MessagesNukedEvent(IEnumerable<uint> set) : EntityEventArgs
{
    public uint[] MessageIds = set.ToArray();
}

