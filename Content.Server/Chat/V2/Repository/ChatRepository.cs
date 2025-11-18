using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Repository;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Replays;

namespace Content.Server.Chat.V2.Repository;

/// <summary>
/// Stores <see cref="IChatEvent"/>, gives them UIDs, and issues <see cref="MessageCreatedEvent"/>.
/// Allows for deletion of messages.
/// </summary>
public sealed class ChatRepositorySystem : EntitySystem
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    // Clocks should start at 1, as 0 indicates "clock not set" or "clock forgotten to be set by bad programmer".
    private uint _nextMessageId = 1;
    private Dictionary<uint, ChatRecord> _messages = new();
    private Dictionary<NetUserId, List<uint>> _playerMessages = new();

    public override void Initialize()
    {
        Refresh();

        _replay.RecordingFinished += _ =>
        {
            // TODO: resolve https://github.com/space-wizards/space-station-14/issues/25485 so we can dump the chat to disc.
            Refresh();
        };
    }

    /// <summary>
    /// Adds an <see cref="IChatEvent"/> to the repo and raises it with a UID for consumption elsewhere.
    /// </summary>
    /// <param name="ev">The event to store and raise</param>
    /// <returns>If storing and raising succeeded.</returns>
    public bool Add(IChatEvent ev)
    {
        if (!_player.TryGetSessionByEntity(ev.Sender, out var session))
        {
            return false;
        }

        var messageId = _nextMessageId;

        _nextMessageId++;

        ev.Id = messageId;

        var storedEv = new ChatRecord
        {
            UserName = session.Name,
            UserId = session.UserId,
            EntityName = Name(ev.Sender),
            StoredEvent = ev
        };

        _messages[messageId] = storedEv;

        CollectionsMarshal.GetValueRefOrAddDefault(_playerMessages, storedEv.UserId, out _)?.Add(messageId);

        RaiseLocalEvent(ev.Sender, new MessageCreatedEvent(ev), true);

        return true;
    }

    /// <summary>
    /// Returns the event associated with a UID, if it exists.
    /// </summary>
    /// <param name="id">The UID of a event.</param>
    /// <returns>The event, if it exists.</returns>
    public IChatEvent? GetEventFor(uint id)
    {
        return _messages.TryGetValue(id, out var record) ? record.StoredEvent : null;
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

        ev.StoredEvent.Message = message;

        RaiseLocalEvent(new MessagePatchedEvent(id, message));

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

        if (_playerMessages.TryGetValue(ev.UserId, out var set))
        {
            set.Remove(id);
        }

        RaiseLocalEvent(new MessageDeletedEvent(id));

        return true;
    }

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userName">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    public bool NukeForUsername(string userName, [NotNullWhen(false)] out string? reason)
    {
        if (!_player.TryGetUserId(userName, out var userId))
        {
            reason = Loc.GetString("command-error-nukechatmessages-usernames-usernamenotexist", ("username", userName));

            return false;
        }

        return NukeForUserId(userId, out reason);
    }

    /// <summary>
    /// Nukes a user's entire chat history from the repo and issues a <see cref="MessageDeletedEvent"/> saying this has
    /// happened.
    /// </summary>
    /// <param name="userId">The user ID to nuke.</param>
    /// <param name="reason">Why nuking failed, if it did.</param>
    /// <returns>If nuking did anything.</returns>
    /// <remarks>Note that this could be a <b>very large</b> event, as we send every single event ID over the wire.
    /// By necessity we can't leak the player-source of chat messages (or if they even have the same origin) because of
    /// client modders who could use that information to cheat/metagrudge/etc >:(</remarks>
    public bool NukeForUserId(NetUserId userId, [NotNullWhen(false)] out string? reason)
    {
        if (!_playerMessages.TryGetValue(userId, out var dict))
        {
            reason = Loc.GetString("command-error-nukechatmessages-usernames-usernamenomessages", ("userId", userId.UserId.ToString()));

            return false;
        }

        foreach (var id in dict)
        {
            _messages.Remove(id);
        }

        var ev = new MessagesNukedEvent(dict);

        CollectionsMarshal.GetValueRefOrAddDefault(_playerMessages, userId, out _)?.Clear();

        RaiseLocalEvent(ev);

        reason = null;

        return true;
    }

    /// <summary>
    /// Dumps held chat storage data and refreshes the repo.
    /// </summary>
    public void Refresh()
    {
        _nextMessageId = 1;
        _messages.Clear();
        _playerMessages.Clear();
    }
}
