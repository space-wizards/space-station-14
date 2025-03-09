using System.Linq;
using System.Runtime.InteropServices;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Repository;

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
public sealed class MessagesNukedEvent(List<uint> set) : EntityEventArgs
{
    public uint[] MessageIds = CollectionsMarshal.AsSpan(set).ToArray();
}

