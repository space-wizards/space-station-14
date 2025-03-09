using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Repository;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2.Repository;

/// <summary>
/// Stores <see cref="IChatEvent"/>, gives them UIDs, and issues <see cref="MessageCreatedEvent"/>.
/// Allows for deletion of messages.
/// </summary>
public sealed class ChatRepository : IChatRepository
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    // Clocks should start at 1, as 0 indicates "clock not set" or "clock forgotten to be set by bad programmer".
    private uint _nextMessageId = 1;
    private readonly Dictionary<uint, ChatMessageWrapper> _messages = new();
    private readonly Dictionary<NetUserId, List<uint>> _playerMessages = new();

    public void Initialize()
    {
        Refresh();

        _replay.RecordingFinished += _ =>
        {
            // TODO: resolve https://github.com/space-wizards/space-station-14/issues/25485 so we can dump the chat to disc.
            Refresh();
        };
    }

    /// <inheritdoc />
    public ChatMessageWrapper? Add(
        FormattedMessage messageContent,
        CommunicationChannelPrototype communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        ChatMessageWrapper? parent,
        HashSet<ICommonSession>? targetSessions = null,
        ChatMessageContext? context = null
        )
    {
        var messageId = _nextMessageId;

        _nextMessageId++;

        if (senderSession == null)
        {
            return new ChatMessageWrapper(
                messageId,
                messageContent,
                communicationChannel,
                senderSession,
                senderEntity,
                parent,
                targetSessions,
                context
            );
        }

        var result = new ChatMessageWrapper(
            messageId,
            messageContent,
            communicationChannel,
            senderSession,
            senderEntity,
            parent,
            targetSessions,
            context
        );

        _messages[messageId] = result;

        var userId = senderSession.UserId;
        CollectionsMarshal.GetValueRefOrAddDefault(_playerMessages, userId, out _)
                          ?.Add(messageId);

        // _entityManager.EventBus.RaiseLocalEvent(userId, new MessageCreatedEvent(ev), true);

        return result;
    }

    /// <inheritdoc />
    public ChatMessageWrapper? GetEventFor(uint id)
    {
        return _messages.TryGetValue(id, out var record)
            ? record
            : null;
    }

    /// <inheritdoc />
    public bool Delete(uint id)
    {
        if (!_messages.Remove(id, out var ev))
        {
            return false;
        }

        if (_playerMessages.TryGetValue(ev.SenderSession!.UserId, out var set))
        {
            set.Remove(id);
        }

        _entityManager.EventBus.RaiseEvent(EventSource.Local, new MessageDeletedEvent(id));

        return true;
    }

    /// <inheritdoc />
    public bool NukeForUsername(string userName, [NotNullWhen(false)] out string? reason)
    {
        if (!_player.TryGetUserId(userName, out var userId))
        {
            reason = Loc.GetString("command-error-nukechatmessages-usernames-usernamenotexist", ("username", userName));

            return false;
        }

        return NukeForUserId(userId, out reason);
    }

    /// <inheritdoc />
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

        _entityManager.EventBus.RaiseEvent(EventSource.Local, ev);

        reason = null;

        return true;
    }

    /// <inheritdoc />
    public void Refresh()
    {
        _nextMessageId = 1;
        _messages.Clear();
        _playerMessages.Clear();
    }
}
