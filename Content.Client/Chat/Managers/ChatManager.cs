using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Chat.UI;
using Content.Client.Ghost;
using Content.Client.Viewport;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers
{
    internal sealed class ChatManager : IChatManager, IPostInjectInit
    {
        private struct SpeechBubbleData
        {
            public string Message;
            public SpeechBubble.SpeechType Type;
        }

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

        /// <summary>
        ///     The max amount of characters an entity can send in one message
        /// </summary>
        public int MaxMessageLength => _cfg.GetCVar(CCVars.ChatMaxMessageLength);

        private readonly List<StoredChatMessage> _history = new();
        public IReadOnlyList<StoredChatMessage> History => _history;

        // currently enabled channel filters set by the user.
        // All values default to on, even if they aren't a filterable chat channel currently.
        // Note that these are persisted here, at the manager,
        // rather than the chatbox so that these settings persist between instances of different
        // chatboxes.
        public ChatChannel ChannelFilters { get; private set; } = (ChatChannel) ushort.MaxValue;

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
        public ChatSelectChannel SelectableChannels { get; private set; }
        public ChatChannel FilterableChannels { get; private set; }

        /// <summary>
        /// For currently disabled chat filters,
        /// unread messages (messages received since the channel has been filtered out).
        /// </summary>
        private readonly Dictionary<ChatChannel, int> _unreadMessages = new();

        public IReadOnlyDictionary<ChatChannel, int> UnreadMessages => _unreadMessages;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientAdminManager _adminMgr = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;

        /// <summary>
        /// Current chat box control. This can be modified, so do not depend on saving a reference to this.
        /// </summary>
        public ChatBox? CurrentChatBox { get; private set; }

        /// <summary>
        /// Invoked when CurrentChatBox is resized (including after setting initial default size)
        /// </summary>
        public event Action<ChatResizedEventArgs>? OnChatBoxResized;

        public event Action<ChatPermissionsUpdatedEventArgs>? ChatPermissionsUpdated;
        public event Action? UnreadMessageCountsUpdated;
        public event Action<StoredChatMessage>? MessageAdded;
        public event Action? FiltersUpdated;

        private Control _speechBubbleRoot = null!;

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

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(OnChatMessage);

            _speechBubbleRoot = new LayoutContainer();
            LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
            _userInterfaceManager.StateRoot.AddChild(_speechBubbleRoot);
            _speechBubbleRoot.SetPositionFirst();
            _stateManager.OnStateChanged += _ => UpdateChannelPermissions();
        }

        public void PostInject()
        {
            _adminMgr.AdminStatusUpdated += UpdateChannelPermissions;
            _playerManager.LocalPlayerChanged += OnLocalPlayerChanged;
            OnLocalPlayerChanged(new LocalPlayerChangedEventArgs(null, _playerManager.LocalPlayer));
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

        // go through all of the various channels and update filter / select permissions
        // appropriately, also enabling them if our enabledChannels dict doesn't have an entry
        // for any newly-granted channels
        private void UpdateChannelPermissions()
        {
            var oldSelectable = SelectableChannels;
            SelectableChannels = default;
            FilterableChannels = default;

            // Can always send console stuff.
            SelectableChannels |= ChatSelectChannel.Console;

            // can always send/recieve OOC
            SelectableChannels |= ChatSelectChannel.OOC;
            FilterableChannels |= ChatChannel.OOC;
            SelectableChannels |= ChatSelectChannel.LOOC;
            FilterableChannels |= ChatChannel.LOOC;

            // can always hear server (nobody can actually send server messages).
            FilterableChannels |= ChatChannel.Server;

            if (_stateManager.CurrentState is GameScreenBase)
            {
                // can always hear local / radio / emote when in the game
                FilterableChannels |= ChatChannel.Local;
                FilterableChannels |= ChatChannel.Whisper;
                FilterableChannels |= ChatChannel.Radio;
                FilterableChannels |= ChatChannel.Emotes;

                // Can only send local / radio / emote when attached to a non-ghost entity.
                // TODO: this logic is iffy (checking if controlling something that's NOT a ghost), is there a better way to check this?
                if (!IsGhost)
                {
                    SelectableChannels |= ChatSelectChannel.Local;
                    SelectableChannels |= ChatSelectChannel.Whisper;
                    SelectableChannels |= ChatSelectChannel.Radio;
                    SelectableChannels |= ChatSelectChannel.Emotes;
                }
            }

            // Only ghosts and admins can send / see deadchat.
            if (_adminMgr.HasFlag(AdminFlags.Admin) || IsGhost)
            {
                FilterableChannels |= ChatChannel.Dead;
                SelectableChannels |= ChatSelectChannel.Dead;
            }

            // only admins can see / filter asay
            if (_adminMgr.HasFlag(AdminFlags.Admin))
            {
                FilterableChannels |= ChatChannel.Admin;
                SelectableChannels |= ChatSelectChannel.Admin;
            }

            // Necessary so that we always have a channel to fall back to.
            DebugTools.Assert((SelectableChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");
            DebugTools.Assert((FilterableChannels & ChatChannel.OOC) != 0, "OOC must always be available");

            // let our chatbox know all the new settings
            ChatPermissionsUpdated?.Invoke(new ChatPermissionsUpdatedEventArgs {OldSelectableChannels = oldSelectable});
        }

        public bool IsGhost => _playerManager.LocalPlayer?.ControlledEntity is {} uid &&
                               uid.IsValid() &&
                               _entityManager.HasComponent<GhostComponent>(uid);

        public void FrameUpdate(FrameEventArgs delta)
        {
            // Update queued speech bubbles.
            if (_queuedSpeechBubbles.Count == 0)
            {
                return;
            }

            foreach (var (entity, queueData) in _queuedSpeechBubbles.ShallowClone())
            {
                if (!_entityManager.EntityExists(entity))
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
        }

        public void SetChatBox(ChatBox chatBox)
        {
            CurrentChatBox = chatBox;
        }

        public void ClearUnfilteredUnreads()
        {
            foreach (var channel in _unreadMessages.Keys.ToArray())
            {
                if ((ChannelFilters & channel) != 0)
                    _unreadMessages.Remove(channel);
            }
        }

        public void ChatBoxOnResized(ChatResizedEventArgs chatResizedEventArgs)
        {
            OnChatBoxResized?.Invoke(chatResizedEventArgs);
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

        public void OnChatBoxTextSubmitted(ChatBox chatBox, ReadOnlyMemory<char> text, ChatSelectChannel channel)
        {
            DebugTools.Assert(chatBox == CurrentChatBox);

            var str = text.ToString();

            switch (channel)
            {
                case ChatSelectChannel.Console:
                    // run locally
                    _consoleHost.ExecuteCommand(text.ToString());
                    break;

                case ChatSelectChannel.LOOC:
                    _consoleHost.ExecuteCommand($"looc \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.OOC:
                    _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Admin:
                    _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Emotes:
                    _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Dead:
                    if (IsGhost)
                        goto case ChatSelectChannel.Local;
                    else if (_adminMgr.HasFlag(AdminFlags.Admin))
                        _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(str)}\"");
                    else
                        Logger.WarningS("chat", "Tried to speak on deadchat without being ghost or admin.");
                    break;

                case ChatSelectChannel.Radio:
                    _consoleHost.ExecuteCommand($"say \";{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Local:
                    _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Whisper:
                    _consoleHost.ExecuteCommand($"whisper \"{CommandParsing.Escape(str)}\"");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        public void OnFilterButtonToggled(ChatChannel channel, bool enabled)
        {
            if (enabled)
            {
                ChannelFilters |= channel;
                _unreadMessages.Remove(channel);
                UnreadMessageCountsUpdated?.Invoke();
            }
            else
            {
                ChannelFilters &= ~channel;
            }

            FiltersUpdated?.Invoke();
        }

        private void OnChatMessage(MsgChatMessage msg)
        {
            // Log all incoming chat to repopulate when filter is un-toggled
            if (!msg.HideChat)
            {
                var storedMessage = new StoredChatMessage(msg);
                _history.Add(storedMessage);
                MessageAdded?.Invoke(storedMessage);

                if (!storedMessage.Read)
                {
                    Logger.Debug($"Message filtered: {storedMessage.Channel}: {storedMessage.Message}");
                    if (!_unreadMessages.TryGetValue(msg.Channel, out var count))
                        count = 0;

                    count += 1;
                    _unreadMessages[msg.Channel] = count;
                    UnreadMessageCountsUpdated?.Invoke();
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
                    if (!IsGhost)
                        break;

                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                    break;

                case ChatChannel.Emotes:
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                    break;
            }
        }

        private void AddSpeechBubble(MsgChatMessage msg, SpeechBubble.SpeechType speechType)
        {
            if (!_entityManager.EntityExists(msg.SenderEntity))
            {
                Logger.WarningS("chat", "Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
                return;
            }

            var messages = SplitMessage(FormattedMessage.RemoveMarkup(msg.Message));

            foreach (var message in messages)
            {
                EnqueueSpeechBubble(msg.SenderEntity, message, speechType);
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

        private void EnqueueSpeechBubble(EntityUid entity, string contents, SpeechBubble.SpeechType speechType)
        {
            // Don't enqueue speech bubbles for other maps. TODO: Support multiple viewports/maps?
            if (_entityManager.GetComponent<TransformComponent>(entity).MapID != _eyeManager.CurrentMap)
                return;

            if (!_queuedSpeechBubbles.TryGetValue(entity, out var queueData))
            {
                queueData = new SpeechBubbleQueueData();
                _queuedSpeechBubbles.Add(entity, queueData);
            }

            queueData.MessageQueue.Enqueue(new SpeechBubbleData
            {
                Message = contents,
                Type = speechType,
            });
        }

        private void CreateSpeechBubble(EntityUid entity, SpeechBubbleData speechData)
        {
            var bubble =
                SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity, _eyeManager, this, _entityManager);

            if (_activeSpeechBubbles.TryGetValue(entity, out var existing))
            {
                // Push up existing bubbles above the mob's head.
                foreach (var existingBubble in existing)
                {
                    existingBubble.VerticalOffset += bubble.ContentHeight;
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

        private sealed class SpeechBubbleQueueData
        {
            /// <summary>
            ///     Time left until the next speech bubble can appear.
            /// </summary>
            public float TimeLeft { get; set; }

            public Queue<SpeechBubbleData> MessageQueue { get; } = new();
        }
    }
}
