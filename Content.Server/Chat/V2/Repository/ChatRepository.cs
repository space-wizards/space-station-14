using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Server.Chat.V2.Repository;

/// <summary>
/// Stores <see cref="IStorableChatEvent"/>, gives them UIDs, and issues them again locally for actioning.
/// </summary>
/// <remarks>
/// This is an <see cref="EntitySystem"/> because:
/// <list type="number"><item>It raises events</item>
/// <item>It needs to extract user UIDs from entities</item>
/// <item>Making this be a manager means more auto-wiring boilerplate</item></list>
/// </remarks>
public sealed class ChatRepository : EntitySystem
{
    // Clocks should start at 1, as 0 indicates "clock not set" or "clock forgotten to be set by bad programmer".
    private uint _clock = 1;
    private Dictionary<uint, IStorableChatEvent> _messages = new();
    private Dictionary<string, Dictionary<uint, IStorableChatEvent>> _playerMessages = new();

    public override void Initialize()
    {
        Refresh();
    }

    /// <summary>
    /// Adds an <see cref="IStorableChatEvent"/> to the repo and raises it with a UID for consumption elsewhere.
    /// </summary>
    /// <param name="ev">The event to store and raise</param>
    /// <returns>If storing and raising succeeded.</returns>
    /// <remarks>This function is not idempotent or thread safe.</remarks>
    public bool Add(IStorableChatEvent ev)
    {
        var i = _clock++;
        ev.SetId(i);
        _messages.Add(i, ev);

        var user = GetUserForEntity(ev);

        if (!_playerMessages.TryGetValue(user, out var set))
        {
            set = new Dictionary<uint, IStorableChatEvent>();
        }

        set.Add(i, ev);
        _playerMessages.Add(user, set);

        RaiseLocalEvent(ev);

        return true;
    }

    /// <summary>
    /// Returns the message associated with a UID, if it exists.
    /// </summary>
    /// <param name="id">The UID of a message.</param>
    /// <returns>The event, if it exists.</returns>
    public IStorableChatEvent? GetMessageFor(uint id)
    {
        return _messages[id];
    }

    /// <summary>
    /// Returns the messages associated with the user that owns an entity.
    /// </summary>
    /// <param name="entity">The entity which has a user we want the messages of.</param>
    /// <returns>An array of messages.</returns>
    public IStorableChatEvent[] GetMessagesFor(EntityUid entity)
    {
        return _playerMessages[GetUserForEntity(entity)].Values.ToArray();
    }

    /// <summary>
    /// Edits a specific message and issues a <see cref="MessagePatchedEvent"/> that says this happened both locally and
    /// on the network. Note that this doesn't replay the message (yet), so translators and mutators won't act on it.
    /// </summary>
    /// <param name="id">The ID to edit</param>
    /// <param name="message">The new message to send</param>
    /// <returns>If patching did anything did anything</returns>
    /// <remarks>Should be used for admining and admemeing only.</remarks>
    public bool Patch(uint id, string message)
    {
        if (!_messages.TryGetValue(id, out var ev))
        {
            return false;
        }

        ev.PatchMessage(message);

        var patch = new MessagePatchedEvent(id, message);
        RaiseLocalEvent(patch);
        RaiseNetworkEvent(patch);

        return true;
    }

    /// <summary>
    /// Deletes a message from the repository and issues a <see cref="MessageDeletedEvent"/> that says this has happened
    /// both locally and on the network.
    /// </summary>
    /// <param name="id">The ID to delete</param>
    /// <returns>If deletion did anything</returns>
    /// <remarks>Should only be used for adminning</remarks>
    public bool Delete(uint id)
    {
        if (!_messages.TryGetValue(id, out var ev))
        {
            return false;
        }

        _messages.Remove(id);

        if (_playerMessages.TryGetValue(GetUserForEntity(ev), out var set))
        {
            set.Remove(id);
        }

        var del = new MessageDeletedEvent(id);
        RaiseLocalEvent(del);
        RaiseNetworkEvent(del);

        return true;
    }

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="user">The user ID to nuke.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.</remarks>
    public bool Nuke(string user)
    {
        if (!_playerMessages.TryGetValue(user, out var dict))
        {
            return false;
        }

        foreach (var id in dict.Keys)
        {
            _messages.Remove(id);
        }

        var ev = new MessagesNukedEvent(dict.Keys);
        RaiseLocalEvent(ev);
        RaiseNetworkEvent(ev);

        _playerMessages.Add(user, new Dictionary<uint, IStorableChatEvent>());

        return true;
    }

    /// <summary>
    /// Dumps held chat storage data and refreshes the repo.
    /// </summary>
    public void Refresh()
    {
        _clock = 1;
        _messages = new Dictionary<uint, IStorableChatEvent>();
        _playerMessages = new Dictionary<string, Dictionary<uint, IStorableChatEvent>>();
    }

    private string GetUserForEntity(IStorableChatEvent ev)
    {
        // Arcane C# interpolated string nonsense ahoy
        return $"{ToPrettyString(ev.GetSender()):user}";
    }

    private string GetUserForEntity(EntityUid uid)
    {
        // Arcane C# interpolated string nonsense ahoy
        return $"{ToPrettyString(uid):user}";
    }
}

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
