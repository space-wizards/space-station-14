using System.Linq;
using System.Numerics;
using Content.Shared.Chat.V2.Repository;
using Content.Shared.GameTicking;
using Robust.Shared.Asynchronous;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server.Chat.V2.Repository;

public interface IStorableChatEvent
{
    public EntityUid GetSender();
    public void SetId(uint id);
    public void PatchMessage(string message);
    public string GetMessageType();
    public string GetMessage();
}

public struct StoredChatEvent
{
    public string userName;
    public string entityName;
    public string message;
    public Vector2 location;
    public string messageType;
    public string map;
}

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
    [Dependency] private readonly IReplayRecordingManager _replay = default!;

    // Clocks should start at 1, as 0 indicates "clock not set" or "clock forgotten to be set by bad programmer".
    private uint _clock = 1;
    private Dictionary<uint, StoredChatEvent> _messages = new();
    private Dictionary<string, Dictionary<uint, StoredChatEvent>> _playerMessages = new();

    public override void Initialize()
    {
        Refresh();

        _replay.RecordingFinished += finished =>
        {
            // TODO: resolve https://github.com/space-wizards/space-station-14/issues/25485 so we can dump the chat to disc.
        };
    }

    /// <summary>
    /// Adds an <see cref="IStorableChatEvent"/> to the repo and raises it with a UID for consumption elsewhere.
    /// </summary>
    /// <param name="ev">The event to store and raise</param>
    /// <returns>If storing and raising succeeded.</returns>
    /// <remarks>This function is not idempotent or thread safe.</remarks>
    public bool Add<T> (T ev) where T : IStorableChatEvent
    {
        var i = _clock++;
        var user = GetUserForEntity(ev);
        ev.SetId(i);

        var location = new Vector2();
        var map = "";

        if (TryComp<TransformComponent>(ev.GetSender(), out var comp))
        {
            location = comp.Coordinates.Position;

            if (comp.MapUid != null)
            {
                map = Name(comp.MapUid.Value);
            }
        }

        var storedEv = new StoredChatEvent
        {
            userName = user,
            entityName = Name(ev.GetSender()),
            message = ev.GetMessage(),
            location = location,
            messageType = ev.GetMessageType(),
            map = map
        };

        _messages.Add(i, storedEv);

        if (!_playerMessages.TryGetValue(user, out var set))
        {
            set = new Dictionary<uint, StoredChatEvent>();
            _playerMessages.Add(user, set);
        }

        set.Add(i, storedEv);

        RaiseLocalEvent(ev.GetSender(), ev, true);

        return true;
    }

    /// <summary>
    /// Returns the message associated with a UID, if it exists.
    /// </summary>
    /// <param name="id">The UID of a message.</param>
    /// <returns>The event, if it exists.</returns>
    public StoredChatEvent? GetMessageFor(uint id)
    {
        return _messages[id];
    }

    /// <summary>
    /// Returns the messages associated with the user that owns an entity.
    /// </summary>
    /// <param name="entity">The entity which has a user we want the messages of.</param>
    /// <returns>An array of messages.</returns>
    public StoredChatEvent[] GetMessagesFor(EntityUid entity)
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

        ev.message = message;

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

        if (_playerMessages.TryGetValue(ev.userName, out var set))
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

        _playerMessages.Add(user, new Dictionary<uint, StoredChatEvent>());

        return true;
    }

    /// <summary>
    /// Dumps held chat storage data and refreshes the repo.
    /// </summary>
    public void Refresh()
    {
        _clock = 1;
        _messages = new Dictionary<uint, StoredChatEvent>();
        _playerMessages = new Dictionary<string, Dictionary<uint, StoredChatEvent>>();
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
