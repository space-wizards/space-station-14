using System;
using System.Collections.Generic;
using Content.Client.Administration;
using Content.Client.GameObjects.Components.Observer;
using Content.Client.Interfaces.Chat;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat
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
        private int _maxMessageLength = 1000;

        public const char ConCmdSlash = '/';
        public const char OOCAlias = '[';
        public const char MeAlias = '@';
        public const char AdminChatAlias = ']';
        public const char RadioAlias = ';';

        private readonly List<StoredChatMessage> _filteredHistory = new();

        // currently enabled channel filters set by the user. If an entry is not in this
        // list it has not been explicitly set yet, thus will default to enabled when it first
        // becomes filterable (added to _filterableChannels)
        // Note that these are persisted here, at the manager,
        // rather than the chatbox so that these settings persist between instances of different
        // chatboxes.
        public readonly Dictionary<ChatChannel, bool> _channelFilters = new();

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
        private readonly HashSet<ChatChannel> _filterableChannels = new();
        private readonly List<ChatChannel> _selectableChannels = new();

        // Flag Enums for holding filtered channels
        private ChatChannel _filteredChannels;

        /// <summary>
        /// For currently disabled chat filters,
        /// unread messages (messages received since the channel has been filtered
        /// out). Never goes above 10 (9+ should be shown when at 10)
        /// </summary>
        private readonly Dictionary<ChatChannel, byte> _unreadMessages = new();

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientAdminManager _adminMgr = default!;

        /// <summary>
        /// Current chat box control. This can be modified, so do not depend on saving a reference to this.
        /// </summary>
        public ChatBox? CurrentChatBox { get; private set; }
        /// <summary>
        /// Invoked when CurrentChatBox is resized (including after setting initial default size)
        /// </summary>
        public event Action<ChatResizedEventArgs>? OnChatBoxResized;

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
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME, OnChatMessage);
            _netManager.RegisterNetMessage<ChatMaxMsgLengthMessage>(ChatMaxMsgLengthMessage.NAME, OnMaxLengthReceived);

            _speechBubbleRoot = new LayoutContainer();
            LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
            _userInterfaceManager.StateRoot.AddChild(_speechBubbleRoot);
            _speechBubbleRoot.SetPositionFirst();

            // When connexion is achieved, request the max chat message length
            _netManager.Connected += RequestMaxLength;
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
            // can always send/recieve OOC
            if (!_selectableChannels.Contains(ChatChannel.OOC))
            {
                _selectableChannels.Add(ChatChannel.OOC);
            }
            AddFilterableChannel(ChatChannel.OOC);

            // can always hear server (nobody can actually send server messages).
            AddFilterableChannel(ChatChannel.Server);

            // can always hear local / radio / emote
            AddFilterableChannel(ChatChannel.Local);
            AddFilterableChannel(ChatChannel.Radio);
            AddFilterableChannel(ChatChannel.Emotes);

            // Can only send local / radio / emote when attached to a non-ghost entity.
            // TODO: this logic is iffy (checking if controlling something that's NOT a ghost), is there a better way to check this?
            if (!_playerManager.LocalPlayer?.ControlledEntity?.HasComponent<GhostComponent>() ?? false)
            {
                _selectableChannels.Add(ChatChannel.Local);
                _selectableChannels.Add(ChatChannel.Radio);
                _selectableChannels.Add(ChatChannel.Emotes);
            }
            else
            {
                _selectableChannels.Remove(ChatChannel.Local);
                _selectableChannels.Remove(ChatChannel.Radio);
                _selectableChannels.Remove(ChatChannel.Emotes);
            }

            // Only ghosts and admins can send / see deadchat.
            // TODO: Should spectators also be able to see deadchat?
            if (_adminMgr.HasFlag(AdminFlags.Admin) ||
                (_playerManager?.LocalPlayer?.ControlledEntity?.HasComponent<GhostComponent>() ?? false))
            {
                AddFilterableChannel(ChatChannel.Dead);
                if (!_selectableChannels.Contains(ChatChannel.Dead))
                {
                    _selectableChannels.Add(ChatChannel.Dead);
                }
            }
            else
            {
                _filterableChannels.Remove(ChatChannel.Dead);
                _selectableChannels.Remove(ChatChannel.Dead);
            }

            // only admins can see / filter asay
            if (_adminMgr.HasFlag(AdminFlags.Admin))
            {
                AddFilterableChannel(ChatChannel.AdminChat);
                if (!_selectableChannels.Contains(ChatChannel.AdminChat))
                {
                    _selectableChannels.Add(ChatChannel.AdminChat);
                }
            }
            else
            {
                _selectableChannels.Remove(ChatChannel.AdminChat);
                _filterableChannels.Remove(ChatChannel.AdminChat);
            }

            // let our chatbox know all the new settings
            CurrentChatBox?.SetChannelPermissions(_selectableChannels, _filterableChannels, _channelFilters, _unreadMessages);
        }

        /// <summary>
        /// Adds the channel to the set of filterable channels, defaulting it as enabled
        /// if it doesn't currently have an explicit enable/disable setting
        /// </summary>
        private void AddFilterableChannel(ChatChannel channel)
        {
            if (!_channelFilters.ContainsKey(channel))
                _channelFilters[channel] = true;
            _filterableChannels.Add(channel);
        }


        public void FrameUpdate(FrameEventArgs delta)
        {
            // Update queued speech bubbles.
            if (_queuedSpeechBubbles.Count == 0)
            {
                return;
            }

            foreach (var (entityUid, queueData) in _queuedSpeechBubbles.ShallowClone())
            {
                if (!_entityManager.TryGetEntity(entityUid, out var entity))
                {
                    _queuedSpeechBubbles.Remove(entityUid);
                    continue;
                }

                queueData.TimeLeft -= delta.DeltaSeconds;
                if (queueData.TimeLeft > 0)
                {
                    continue;
                }

                if (queueData.MessageQueue.Count == 0)
                {
                    _queuedSpeechBubbles.Remove(entityUid);
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
            if (CurrentChatBox != null)
            {
                CurrentChatBox.TextSubmitted -= OnChatBoxTextSubmitted;
                CurrentChatBox.FilterToggled -= OnFilterButtonToggled;
                CurrentChatBox.OnResized -= ChatBoxOnResized;
            }

            CurrentChatBox = chatBox;
            if (CurrentChatBox != null)
            {
                CurrentChatBox.TextSubmitted += OnChatBoxTextSubmitted;
                CurrentChatBox.FilterToggled += OnFilterButtonToggled;
                CurrentChatBox.OnResized += ChatBoxOnResized;

                CurrentChatBox.SetChannelPermissions(_selectableChannels, _filterableChannels, _channelFilters, _unreadMessages);
            }

            RepopulateChat(_filteredHistory);
        }

        private void ChatBoxOnResized(ChatResizedEventArgs chatResizedEventArgs)
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

        private void WriteChatMessage(StoredChatMessage message)
        {
            Logger.Debug($"{message.Channel}: {message.Message}");

            if (IsFiltered(message.Channel))
            {
                Logger.Debug($"Message filtered: {message.Channel}: {message.Message}");
                // accumulate unread
                if (message.Read) return;
                if (!_unreadMessages.TryGetValue(message.Channel, out var count))
                {
                    count = 0;
                }
                count = (byte) Math.Min(count + 1, 10);
                _unreadMessages[message.Channel] = count;
                CurrentChatBox?.UpdateUnreadMessageCounts(_unreadMessages);
                return;
            }

            var color = Color.DarkGray;
            var messageText = message.Message;
            if (!string.IsNullOrEmpty(message.MessageWrap))
            {
                messageText = string.Format(message.MessageWrap, messageText);
            }

            if (message.MessageColorOverride != Color.Transparent)
            {
                color = message.MessageColorOverride;
            }
            else
            {
                color = message.Channel switch
                {
                    ChatChannel.Server => Color.Orange,
                    ChatChannel.Radio => Color.Green,
                    ChatChannel.OOC => Color.LightSkyBlue,
                    ChatChannel.Dead => Color.MediumPurple,
                    ChatChannel.AdminChat => Color.Red,
                    _ => color
                };
            }

            if (CurrentChatBox == null) return;
            CurrentChatBox.AddLine(messageText, message.Channel, color);
            // TODO: Can make this "smarter" later by only setting it false when the message has been scrolled to
            message.Read = true;
        }

        private void OnChatBoxTextSubmitted(ChatBox chatBox, string text)
        {
            DebugTools.Assert(chatBox == CurrentChatBox);

            if (string.IsNullOrWhiteSpace(text))
                return;

            // Check if message is longer than the character limit
            if (text.Length > _maxMessageLength)
            {
                if (CurrentChatBox != null)
                {
                    string locWarning = Loc.GetString("chat-manager-max-message-length",
                                            ("maxMessageLength", _maxMessageLength));
                    CurrentChatBox.AddLine(locWarning, ChatChannel.Server, Color.Orange);
                    CurrentChatBox.ClearOnEnter = false; // The text shouldn't be cleared if it hasn't been sent
                }
                return;
            }

            switch (text[0])
            {
                case ConCmdSlash:
                {
                    // run locally
                    var conInput = text.Substring(1);
                    _consoleHost.ExecuteCommand(conInput);
                    break;
                }
                case OOCAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(conInput)}\"");
                    break;
                }
                case AdminChatAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    if (_adminMgr.HasFlag(AdminFlags.Admin))
                    {
                        _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(conInput)}\"");
                    }
                    else
                    {
                        _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(conInput)}\"");
                    }

                    break;
                }
                case MeAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(conInput)}\"");
                    break;
                }
                default:
                {
                    var conInput = CurrentChatBox?.DefaultChatFormat != null
                        ? string.Format(CurrentChatBox.DefaultChatFormat, CommandParsing.Escape(text))
                        : text;
                    _consoleHost.ExecuteCommand(conInput);
                    break;
                }
            }
        }

        private void OnFilterButtonToggled(ChatChannel channel, bool enabled)
        {
            if (enabled)
            {
                _channelFilters[channel] = true;
                _filteredChannels &= ~channel;
                _unreadMessages.Remove(channel);
                CurrentChatBox?.UpdateUnreadMessageCounts(_unreadMessages);
            }
            else
            {
                _channelFilters[channel] = false;
                _filteredChannels |= channel;
            }

            RepopulateChat(_filteredHistory);
        }

        private void RepopulateChat(IEnumerable<StoredChatMessage> filteredMessages)
        {
            if (CurrentChatBox == null)
            {
                return;
            }

            CurrentChatBox.Contents.Clear();

            foreach (var msg in filteredMessages)
            {
                WriteChatMessage(msg);
            }
        }

        private void OnChatMessage(MsgChatMessage msg)
        {
            // Log all incoming chat to repopulate when filter is un-toggled
            var storedMessage = new StoredChatMessage(msg);
            _filteredHistory.Add(storedMessage);
            WriteChatMessage(storedMessage);

            // Local messages that have an entity attached get a speech bubble.
            if (msg.SenderEntity == default)
                return;

            switch (msg.Channel)
            {
                case ChatChannel.Local:
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                    break;

                case ChatChannel.Dead:
                    if (!_playerManager.LocalPlayer?.ControlledEntity?.HasComponent<GhostComponent>() ?? true)
                        break;

                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                    break;

                case ChatChannel.Emotes:
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                    break;
            }
        }

        private void OnMaxLengthReceived(ChatMaxMsgLengthMessage msg)
        {
            _maxMessageLength = msg.MaxMessageLength;
        }

        private void RequestMaxLength(object? sender, NetChannelArgs args)
        {
            ChatMaxMsgLengthMessage msg = _netManager.CreateNetMessage<ChatMaxMsgLengthMessage>();
            _netManager.ClientSendMessage(msg);
        }

        private void AddSpeechBubble(MsgChatMessage msg, SpeechBubble.SpeechType speechType)
        {
            if (!_entityManager.TryGetEntity(msg.SenderEntity, out var entity))
            {
                Logger.WarningS("chat", "Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
                return;
            }

            var messages = SplitMessage(FormattedMessage.RemoveMarkup(msg.Message));

            foreach (var message in messages)
            {
                EnqueueSpeechBubble(entity, message, speechType);
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

        private void EnqueueSpeechBubble(IEntity entity, string contents, SpeechBubble.SpeechType speechType)
        {
            if (!_queuedSpeechBubbles.TryGetValue(entity.Uid, out var queueData))
            {
                queueData = new SpeechBubbleQueueData();
                _queuedSpeechBubbles.Add(entity.Uid, queueData);
            }

            queueData.MessageQueue.Enqueue(new SpeechBubbleData
            {
                Message = contents,
                Type = speechType,
            });
        }

        private void CreateSpeechBubble(IEntity entity, SpeechBubbleData speechData)
        {
            var bubble =
                SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity, _eyeManager, this);

            if (_activeSpeechBubbles.TryGetValue(entity.Uid, out var existing))
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
                _activeSpeechBubbles.Add(entity.Uid, existing);
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

        private bool IsFiltered(ChatChannel channel)
        {
            return _filteredChannels.HasFlag(channel);
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
