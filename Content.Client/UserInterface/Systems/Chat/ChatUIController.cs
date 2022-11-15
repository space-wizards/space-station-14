using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.TypingIndicator;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed class ChatUIController : UIController
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _manager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;

    [UISystemDependency] private readonly ExamineSystem? _examine = default;
    [UISystemDependency] private readonly GhostSystem? _ghost = default;
    [UISystemDependency] private readonly TypingIndicatorSystem? _typingIndicator = default;

    private ISawmill _sawmill = default!;

    public const char AliasLocal = '.';
    public const char AliasConsole = '/';
    public const char AliasDead = '\\';
    public const char AliasLOOC = '(';
    public const char AliasOOC = '[';
    public const char AliasEmotes = '@';
    public const char AliasAdmin = ']';
    public const char AliasRadio = ';';
    public const char AliasWhisper = ',';

    public static readonly Dictionary<char, ChatSelectChannel> PrefixToChannel = new()
    {
        {AliasLocal, ChatSelectChannel.Local},
        {AliasWhisper, ChatSelectChannel.Whisper},
        {AliasConsole, ChatSelectChannel.Console},
        {AliasLOOC, ChatSelectChannel.LOOC},
        {AliasOOC, ChatSelectChannel.OOC},
        {AliasEmotes, ChatSelectChannel.Emotes},
        {AliasAdmin, ChatSelectChannel.Admin},
        {AliasRadio, ChatSelectChannel.Radio},
        {AliasDead, ChatSelectChannel.Dead}
    };

    public static readonly Dictionary<ChatSelectChannel, char> ChannelPrefixes =
        PrefixToChannel.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>
    ///     The max amount of chars allowed to fit in a single speech bubble.
    /// </summary>
    private const int SingleBubbleCharLimit = 100;

    /// <summary>
    ///     Base queue delay each speech bubble has.
    /// </summary>
    private const float BubbleDelayBase = 0.2f;

    /// <summary>
    ///     Factor multiplied by speech bubble char length to add to delay.
    /// </summary>
    private const float BubbleDelayFactor = 0.8f / SingleBubbleCharLimit;

    /// <summary>
    ///     The max amount of speech bubbles over a single entity at once.
    /// </summary>
    private const int SpeechBubbleCap = 4;

    private LayoutContainer _speechBubbleRoot = default!;

    /// <summary>
    ///     Speech bubbles that are currently visible on screen.
    ///     We track them to push them up when new ones get added.
    /// </summary>
    private readonly Dictionary<EntityUid, List<SpeechBubble>> _activeSpeechBubbles =
        new();

    /// <summary>
    ///     Speech bubbles that are to-be-sent because of the "rate limit" they have.
    /// </summary>
    private readonly Dictionary<EntityUid, SpeechBubbleQueueData> _queuedSpeechBubbles
        = new();

    private readonly HashSet<ChatBox> _chats = new();

    /// <summary>
    ///     The max amount of characters an entity can send in one message
    /// </summary>
    public int MaxMessageLength => _config.GetCVar(CCVars.ChatMaxMessageLength);

    /// <summary>
    /// For currently disabled chat filters,
    /// unread messages (messages received since the channel has been filtered out).
    /// </summary>
    private readonly Dictionary<ChatChannel, int> _unreadMessages = new();

    public readonly List<StoredChatMessage> History = new();

    // Maintains which channels a client should be able to filter (for showing in the chatbox)
    // and select (for attempting to send on).
    // This may not always actually match with what the server will actually allow them to
    // send / receive on, it is only what the user can select in the UI. For example,
    // if a user is silenced from speaking for some reason this may still contain ChatChannel.Local, it is left up
    // to the server to handle invalid attempts to use particular channels and not send messages for
    // channels the user shouldn't be able to hear.
    //
    // Note that Command is an available selection in the chatbox channel selector,
    // which is not actually a chat channel but is always available.
    public ChatSelectChannel CanSendChannels { get; private set; }
    public ChatChannel FilterableChannels { get; private set; }
    public ChatSelectChannel SelectableChannels { get; private set; }
    private ChatSelectChannel PreferredChannel { get; set; } = ChatSelectChannel.OOC;

    public event Action<ChatSelectChannel>? CanSendChannelsChanged;
    public event Action<ChatChannel>? FilterableChannelsChanged;
    public event Action<ChatSelectChannel>? SelectableChannelsChanged;
    public event Action<ChatChannel, int?>? UnreadMessageCountsUpdated;
    public event Action<StoredChatMessage>? MessageAdded;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
        _admin.AdminStatusUpdated += UpdateChannelPermissions;
        _player.LocalPlayerChanged += OnLocalPlayerChanged;
        _state.OnStateChanged += StateChanged;
        _net.RegisterNetMessage<MsgChatMessage>(OnChatMessage);

        _speechBubbleRoot = new LayoutContainer();

        OnLocalPlayerChanged(new LocalPlayerChangedEventArgs(null, _player.LocalPlayer));

        _input.SetInputCommand(ContentKeyFunctions.FocusChat,
            InputCmdHandler.FromDelegate(_ => FocusChat()));

        _input.SetInputCommand(ContentKeyFunctions.FocusLocalChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Local)));

        _input.SetInputCommand(ContentKeyFunctions.FocusWhisperChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Whisper)));

        _input.SetInputCommand(ContentKeyFunctions.FocusOOC,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.OOC)));

        _input.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Admin)));

        _input.SetInputCommand(ContentKeyFunctions.FocusRadio,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Radio)));

        _input.SetInputCommand(ContentKeyFunctions.FocusDeadChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Dead)));

        _input.SetInputCommand(ContentKeyFunctions.FocusConsoleChat,
            InputCmdHandler.FromDelegate(_ => FocusChannel(ChatSelectChannel.Console)));

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(true)));

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(false)));
    }

    public void SetMainChat(bool setting)
    {
        // This isn't very nice to look at.
        var widget = UIManager.ActiveScreen?.GetWidget<ChatBox>();
        if (widget == null)
        {
            widget = UIManager.ActiveScreen?.GetWidget<ResizableChatBox>();
            if (widget == null)
            {
                return;
            }
        }

        widget.Main = setting;
    }

    private void FocusChat()
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.Focus();
            break;
        }
    }

    private void FocusChannel(ChatSelectChannel channel)
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.Focus(channel);
            break;
        }
    }

    private void CycleChatChannel(bool forward)
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.CycleChatChannel(forward);
            break;
        }
    }

    private void StateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            PreferredChannel = ChatSelectChannel.Local;
        }

        UpdateChannelPermissions();
    }

    public void SetSpeechBubbleRoot(LayoutContainer root)
    {
        _speechBubbleRoot.Orphan();
        root.AddChild(_speechBubbleRoot);
        LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        _speechBubbleRoot.SetPositionLast();
    }

    private void OnLocalPlayerChanged(LocalPlayerChangedEventArgs obj)
    {
        if (obj.OldPlayer != null)
        {
            obj.OldPlayer.EntityAttached -= OnLocalPlayerEntityAttached;
            obj.OldPlayer.EntityDetached -= OnLocalPlayerEntityDetached;
        }

        if (obj.NewPlayer != null)
        {
            obj.NewPlayer.EntityAttached += OnLocalPlayerEntityAttached;
            obj.NewPlayer.EntityDetached += OnLocalPlayerEntityDetached;
        }

        UpdateChannelPermissions();
    }

    private void OnLocalPlayerEntityAttached(EntityAttachedEventArgs obj)
    {
        UpdateChannelPermissions();
    }

    private void OnLocalPlayerEntityDetached(EntityDetachedEventArgs obj)
    {
        UpdateChannelPermissions();
    }

    private void AddSpeechBubble(MsgChatMessage msg, SpeechBubble.SpeechType speechType)
    {
        if (!_entities.EntityExists(msg.SenderEntity))
        {
            _sawmill.Debug("Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
            return;
        }

        // msg.Message should be the string that a user sent over text, without any added markup.
        var messages = SplitMessage(msg.Message);

        foreach (var message in messages)
        {
            EnqueueSpeechBubble(msg.SenderEntity, message, speechType);
        }
    }

    private void CreateSpeechBubble(EntityUid entity, SpeechBubbleData speechData)
    {
        var bubble =
            SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity, _eye, _manager, _entities);

        bubble.OnDied += SpeechBubbleDied;

        if (_activeSpeechBubbles.TryGetValue(entity, out var existing))
        {
            // Push up existing bubbles above the mob's head.
            foreach (var existingBubble in existing)
            {
                existingBubble.VerticalOffset += bubble.ContentSize.Y;
            }
        }
        else
        {
            existing = new List<SpeechBubble>();
            _activeSpeechBubbles.Add(entity, existing);
        }

        existing.Add(bubble);
        _speechBubbleRoot.AddChild(bubble);

        if (existing.Count > SpeechBubbleCap)
        {
            // Get the oldest to start fading fast.
            var last = existing[0];
            last.FadeNow();
        }
    }

    private void SpeechBubbleDied(EntityUid entity, SpeechBubble bubble)
    {
        RemoveSpeechBubble(entity, bubble);
    }

    private void EnqueueSpeechBubble(EntityUid entity, string contents, SpeechBubble.SpeechType speechType)
    {
        // Don't enqueue speech bubbles for other maps. TODO: Support multiple viewports/maps?
        if (_entities.GetComponent<TransformComponent>(entity).MapID != _eye.CurrentMap)
            return;

        if (!_queuedSpeechBubbles.TryGetValue(entity, out var queueData))
        {
            queueData = new SpeechBubbleQueueData();
            _queuedSpeechBubbles.Add(entity, queueData);
        }

        queueData.MessageQueue.Enqueue(new SpeechBubbleData(contents, speechType));
    }

    public void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble)
    {
        bubble.Dispose();

        var list = _activeSpeechBubbles[entityUid];
        list.Remove(bubble);

        if (list.Count == 0)
        {
            _activeSpeechBubbles.Remove(entityUid);
        }
    }

    public static string GetChannelSelectorName(ChatSelectChannel channelSelector)
    {
        return channelSelector.ToString();
    }

    private void UpdateChannelPermissions()
    {
        CanSendChannels = default;
        FilterableChannels = default;

        // Can always send console stuff.
        CanSendChannels |= ChatSelectChannel.Console;

        // can always send/recieve OOC
        CanSendChannels |= ChatSelectChannel.OOC;
        CanSendChannels |= ChatSelectChannel.LOOC;
        FilterableChannels |= ChatChannel.OOC;
        FilterableChannels |= ChatChannel.LOOC;

        // can always hear server (nobody can actually send server messages).
        FilterableChannels |= ChatChannel.Server;

        if (_state.CurrentState is GameplayStateBase)
        {
            // can always hear local / radio / emote when in the game
            FilterableChannels |= ChatChannel.Local;
            FilterableChannels |= ChatChannel.Whisper;
            FilterableChannels |= ChatChannel.Radio;
            FilterableChannels |= ChatChannel.Emotes;

            // Can only send local / radio / emote when attached to a non-ghost entity.
            // TODO: this logic is iffy (checking if controlling something that's NOT a ghost), is there a better way to check this?
            if (_ghost is not {IsGhost: true})
            {
                CanSendChannels |= ChatSelectChannel.Local;
                CanSendChannels |= ChatSelectChannel.Whisper;
                CanSendChannels |= ChatSelectChannel.Radio;
                CanSendChannels |= ChatSelectChannel.Emotes;
            }
        }

        // Only ghosts and admins can send / see deadchat.
        if (_admin.HasFlag(AdminFlags.Admin) || _ghost is {IsGhost: true})
        {
            FilterableChannels |= ChatChannel.Dead;
            CanSendChannels |= ChatSelectChannel.Dead;
        }

        // only admins can see / filter asay
        if (_admin.HasFlag(AdminFlags.Admin))
        {
            FilterableChannels |= ChatChannel.Admin;
            CanSendChannels |= ChatSelectChannel.Admin;
        }

        SelectableChannels = CanSendChannels;

        // Necessary so that we always have a channel to fall back to.
        DebugTools.Assert((CanSendChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((FilterableChannels & ChatChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((SelectableChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");

        // let our chatbox know all the new settings
        CanSendChannelsChanged?.Invoke(CanSendChannels);
        FilterableChannelsChanged?.Invoke(FilterableChannels);
        SelectableChannelsChanged?.Invoke(SelectableChannels);
    }

    public void ClearUnfilteredUnreads(ChatChannel channels)
    {
        foreach (var channel in _unreadMessages.Keys.ToArray())
        {
            if ((channels & channel) == 0)
                continue;

            _unreadMessages[channel] = 0;
            UnreadMessageCountsUpdated?.Invoke(channel, 0);
        }
    }

    public override void FrameUpdate(FrameEventArgs delta)
    {
        UpdateQueuedSpeechBubbles(delta);
    }

    private void UpdateQueuedSpeechBubbles(FrameEventArgs delta)
    {
        // Update queued speech bubbles.
        if (_queuedSpeechBubbles.Count == 0 || _examine == null)
        {
            return;
        }

        foreach (var (entity, queueData) in _queuedSpeechBubbles.ShallowClone())
        {
            if (!_entities.EntityExists(entity))
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            queueData.TimeLeft -= delta.DeltaSeconds;
            if (queueData.TimeLeft > 0)
            {
                continue;
            }

            if (queueData.MessageQueue.Count == 0)
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            var msg = queueData.MessageQueue.Dequeue();

            queueData.TimeLeft += BubbleDelayBase + msg.Message.Length * BubbleDelayFactor;

            // We keep the queue around while it has 0 items. This allows us to keep the timer.
            // When the timer hits 0 and there's no messages left, THEN we can clear it up.
            CreateSpeechBubble(entity, msg);
        }

        var player = _player.LocalPlayer?.ControlledEntity;
        var predicate = static (EntityUid uid, (EntityUid compOwner, EntityUid? attachedEntity) data)
            => uid == data.compOwner || uid == data.attachedEntity;
        var playerPos = player != null
            ? _entities.GetComponent<TransformComponent>(player.Value).MapPosition
            : MapCoordinates.Nullspace;

        var occluded = player != null && _examine.IsOccluded(player.Value);

        foreach (var (ent, bubs) in _activeSpeechBubbles)
        {
            if (_entities.Deleted(ent))
            {
                SetBubbles(bubs, false);
                continue;
            }

            if (ent == player)
            {
                SetBubbles(bubs, true);
                continue;
            }

            var otherPos = _entities.GetComponent<TransformComponent>(ent).MapPosition;

            if (occluded && !ExamineSystemShared.InRangeUnOccluded(
                    playerPos,
                    otherPos, 0f,
                    (ent, player), predicate))
            {
                SetBubbles(bubs, false);
                continue;
            }

            SetBubbles(bubs, true);
        }
    }

    private void SetBubbles(List<SpeechBubble> bubbles, bool visible)
    {
        foreach (var bubble in bubbles)
        {
            bubble.Visible = visible;
        }
    }

    private List<string> SplitMessage(string msg)
    {
        // Split message into words separated by spaces.
        var words = msg.Split(' ');
        var messages = new List<string>();
        var currentBuffer = new List<string>();

        // Really shoddy way to approximate word length.
        // Yes, I am aware of all the crimes here.
        // TODO: Improve this to use actual glyph width etc..
        var currentWordLength = 0;
        foreach (var word in words)
        {
            // +1 for the space.
            currentWordLength += word.Length + 1;

            if (currentWordLength > SingleBubbleCharLimit)
            {
                // Too long for the current speech bubble, flush it.
                messages.Add(string.Join(" ", currentBuffer));
                currentBuffer.Clear();

                currentWordLength = word.Length;

                if (currentWordLength > SingleBubbleCharLimit)
                {
                    // Word is STILL too long.
                    // Truncate it with an ellipse.
                    messages.Add($"{word.Substring(0, SingleBubbleCharLimit - 3)}...");
                    currentWordLength = 0;
                    continue;
                }
            }

            currentBuffer.Add(word);
        }

        if (currentBuffer.Count != 0)
        {
            // Don't forget the last bubble.
            messages.Add(string.Join(" ", currentBuffer));
        }

        return messages;
    }

    public ChatSelectChannel MapLocalIfGhost(ChatSelectChannel channel)
    {
        if (channel == ChatSelectChannel.Local && _ghost is {IsGhost: true})
            return ChatSelectChannel.Dead;

        return channel;
    }

    public (ChatSelectChannel channel, ReadOnlyMemory<char> text) SplitInputContents(string inputText)
    {
        var text = inputText.AsMemory().Trim();
        if (text.Length == 0)
            return default;

        var prefixChar = text.Span[0];
        var channel = PrefixToChannel.GetValueOrDefault(prefixChar);

        if ((CanSendChannels & channel) != 0)
            // Cut off prefix if it's valid and we can use the channel in question.
            text = text[1..];
        else
            channel = 0;

        channel = MapLocalIfGhost(channel);

        // Trim from start again to cut out any whitespace between the prefix and message, if any.
        return (channel, text.TrimStart());
    }

    public void SendMessage(ChatBox box, ChatSelectChannel channel)
    {
        _typingIndicator?.ClientSubmittedChatText();

        if (!string.IsNullOrWhiteSpace(box.ChatInput.Input.Text))
        {
            var (prefixChannel, text) = SplitInputContents(box.ChatInput.Input.Text);

            // Check if message is longer than the character limit
            if (text.Length > MaxMessageLength)
            {
                var locWarning = Loc.GetString("chat-manager-max-message-length",
                    ("maxMessageLength", MaxMessageLength));
                box.AddLine(locWarning, Color.Orange);
                return;
            }

            _manager.SendMessage(text, prefixChannel == 0 ? channel : prefixChannel);
        }

        box.ChatInput.Input.Clear();
        box.UpdateSelectedChannel();
        box.ChatInput.Input.ReleaseKeyboardFocus();
    }

    private void OnChatMessage(MsgChatMessage msg)
    {
        // Log all incoming chat to repopulate when filter is un-toggled
        if (!msg.HideChat)
        {
            var storedMessage = new StoredChatMessage(msg);
            History.Add(storedMessage);
            MessageAdded?.Invoke(storedMessage);

            if (!storedMessage.Read)
            {
                _sawmill.Debug($"Message filtered: {storedMessage.Channel}: {storedMessage.Message}");
                if (!_unreadMessages.TryGetValue(msg.Channel, out var count))
                    count = 0;

                count += 1;
                _unreadMessages[msg.Channel] = count;
                UnreadMessageCountsUpdated?.Invoke(msg.Channel, count);
            }
        }

        // Local messages that have an entity attached get a speech bubble.
        if (msg.SenderEntity == default)
            return;

        switch (msg.Channel)
        {
            case ChatChannel.Local:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Whisper:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Whisper);
                break;

            case ChatChannel.Dead:
                if (_ghost is not {IsGhost: true})
                    break;

                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Emotes:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                break;
        }
    }

    public char GetPrefixFromChannel(ChatSelectChannel channel)
    {
        return ChannelPrefixes.GetValueOrDefault(channel);
    }

    public void RegisterChat(ChatBox chat)
    {
        _chats.Add(chat);
    }

    public void UnregisterChat(ChatBox chat)
    {
        _chats.Remove(chat);
    }

    public ChatSelectChannel GetPreferredChannel()
    {
        return MapLocalIfGhost(PreferredChannel);
    }

    public void NotifyChatTextChange()
    {
        _typingIndicator?.ClientChangedChatText();
    }

    private readonly record struct SpeechBubbleData(string Message, SpeechBubble.SpeechType Type);

    private sealed class SpeechBubbleQueueData
    {
        /// <summary>
        ///     Time left until the next speech bubble can appear.
        /// </summary>
        public float TimeLeft { get; set; }

        public Queue<SpeechBubbleData> MessageQueue { get; } = new();
    }
}
