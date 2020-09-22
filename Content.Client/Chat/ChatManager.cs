using System;
using System.Collections.Generic;
using Content.Client.Interfaces.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    internal sealed class ChatManager : IChatManager
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

        private const char ConCmdSlash = '/';
        private const char OOCAlias = '[';
        private const char MeAlias = '@';
        private const char AdminChatAlias = ']';

        private readonly List<StoredChatMessage> filteredHistory = new List<StoredChatMessage>();

        // Filter Button States
        private bool _allState;
        private bool _localState;
        private bool _oocState;
        private bool _adminState;

        // Flag Enums for holding filtered channels
        private ChatChannel _filteredChannels;

        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IClientConsole _console = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientConGroupController _groupController = default!;

        private ChatBox _currentChatBox;
        private Control _speechBubbleRoot;

        /// <summary>
        ///     Speech bubbles that are currently visible on screen.
        ///     We track them to push them up when new ones get added.
        /// </summary>
        private readonly Dictionary<EntityUid, List<SpeechBubble>> _activeSpeechBubbles =
            new Dictionary<EntityUid, List<SpeechBubble>>();

        /// <summary>
        ///     Speech bubbles that are to-be-sent because of the "rate limit" they have.
        /// </summary>
        private readonly Dictionary<EntityUid, SpeechBubbleQueueData> _queuedSpeechBubbles
            = new Dictionary<EntityUid, SpeechBubbleQueueData>();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME, _onChatMessage);
            _netManager.RegisterNetMessage<ChatMaxMsgLengthMessage>(ChatMaxMsgLengthMessage.NAME, _onMaxLengthReceived);

            _speechBubbleRoot = new LayoutContainer();
            LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
            _userInterfaceManager.StateRoot.AddChild(_speechBubbleRoot);
            _speechBubbleRoot.SetPositionFirst();

            // When connexion is achieved, request the max chat message length
            _netManager.Connected += new EventHandler<NetChannelArgs>(RequestMaxLength);
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
            if (_currentChatBox != null)
            {
                _currentChatBox.TextSubmitted -= _onChatBoxTextSubmitted;
                _currentChatBox.FilterToggled -= _onFilterButtonToggled;
            }

            _currentChatBox = chatBox;
            if (_currentChatBox != null)
            {
                _currentChatBox.TextSubmitted += _onChatBoxTextSubmitted;
                _currentChatBox.FilterToggled += _onFilterButtonToggled;
            }

            RepopulateChat(filteredHistory);
            _currentChatBox.AllButton.Pressed = !_allState;
            _currentChatBox.LocalButton.Pressed = !_localState;
            _currentChatBox.OOCButton.Pressed = !_oocState;
            if(chatBox.AdminButton != null)
                _currentChatBox.AdminButton.Pressed = !_adminState;
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
                return;
            }

            var color = Color.DarkGray;
            var messageText = message.Message;
            if (!string.IsNullOrEmpty(message.MessageWrap))
            {
                messageText = string.Format(message.MessageWrap, messageText);
            }

            switch (message.Channel)
            {
                case ChatChannel.Server:
                    color = Color.Orange;
                    break;
                case ChatChannel.OOC:
                    color = Color.LightSkyBlue;
                    break;
                case ChatChannel.Dead:
                    color = Color.MediumPurple;
                    break;
                case ChatChannel.AdminChat:
                    color = Color.Red;
                    break;
            }

            _currentChatBox?.AddLine(messageText, message.Channel, color);
        }

        private void _onChatBoxTextSubmitted(ChatBox chatBox, string text)
        {
            DebugTools.Assert(chatBox == _currentChatBox);

            if (string.IsNullOrWhiteSpace(text))
                return;

            // Check if message is longer than the character limit
            if (text.Length > _maxMessageLength)
            {
                string locWarning = Loc.GetString("Your message exceeds {0} character limit", _maxMessageLength);
                _currentChatBox?.AddLine(locWarning, ChatChannel.Server, Color.Orange);
                _currentChatBox.ClearOnEnter = false;   // The text shouldn't be cleared if it hasn't been sent
                return;
            }

            switch (text[0])
            {
                case ConCmdSlash:
                {
                    // run locally
                    var conInput = text.Substring(1);
                    _console.ProcessCommand(conInput);
                    break;
                }
                case OOCAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    _console.ProcessCommand($"ooc \"{CommandParsing.Escape(conInput)}\"");
                    break;
                }
                case AdminChatAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    if (_groupController.CanCommand("asay")){
                        _console.ProcessCommand($"asay \"{CommandParsing.Escape(conInput)}\"");
                    }
                    else
                    {
                        _console.ProcessCommand($"ooc \"{CommandParsing.Escape(conInput)}\"");
                    }
                    break;
                }
                case MeAlias:
                {
                    var conInput = text.Substring(1);
                    if (string.IsNullOrWhiteSpace(conInput))
                        return;
                    _console.ProcessCommand($"me \"{CommandParsing.Escape(conInput)}\"");
                    break;
                }
                default:
                {
                    var conInput = _currentChatBox.DefaultChatFormat != null
                        ? string.Format(_currentChatBox.DefaultChatFormat, CommandParsing.Escape(text))
                        : text;
                    _console.ProcessCommand(conInput);
                    break;
                }
            }
        }

        private void _onFilterButtonToggled(ChatBox chatBox, BaseButton.ButtonToggledEventArgs e)
        {
            switch (e.Button.Name)
            {
                case "Local":
                    _localState = !_localState;
                    if (_localState)
                    {
                        _filteredChannels |= ChatChannel.Local;
                        break;
                    }
                    else
                    {
                        _filteredChannels &= ~ChatChannel.Local;
                        break;
                    }

                case "OOC":
                    _oocState = !_oocState;
                    if (_oocState)
                    {
                        _filteredChannels |= ChatChannel.OOC;
                        break;
                    }
                    else
                    {
                        _filteredChannels &= ~ChatChannel.OOC;
                        break;
                    }
                case "Admin":
                    _adminState = !_adminState;
                    if (_adminState)
                    {
                        _filteredChannels |= ChatChannel.AdminChat;
                        break;
                    }
                    else
                    {
                        _filteredChannels &= ~ChatChannel.AdminChat;
                        break;
                    }

                case "ALL":
                    chatBox.LocalButton.Pressed ^= true;
                    chatBox.OOCButton.Pressed ^= true;
                    if (chatBox.AdminButton != null)
                        chatBox.AdminButton.Pressed ^= true;
                    _allState = !_allState;
                    break;
            }

            RepopulateChat(filteredHistory);
        }

        private void RepopulateChat(IEnumerable<StoredChatMessage> filteredMessages)
        {
            _currentChatBox.Contents.Clear();

            foreach (var msg in filteredMessages)
            {
                WriteChatMessage(msg);
            }
        }

        private void _onChatMessage(MsgChatMessage msg)
        {
            // Log all incoming chat to repopulate when filter is un-toggled
            var storedMessage = new StoredChatMessage(msg);
            filteredHistory.Add(storedMessage);
            WriteChatMessage(storedMessage);

            // Local messages that have an entity attached get a speech bubble.
            if (msg.SenderEntity == default)
                return;

            switch (msg.Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                    break;

                case ChatChannel.Emotes:
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                    break;
            }
        }

        private void _onMaxLengthReceived(ChatMaxMsgLengthMessage msg)
        {
            _maxMessageLength = msg.MaxMessageLength;
        }

        private void RequestMaxLength(object sender, NetChannelArgs args)
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

            var messages = SplitMessage(msg.Message);

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
            var bubble = SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity, _eyeManager, this);

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
            // _allState works as inverter.
            return _allState ^ _filteredChannels.HasFlag(channel);
        }

        private sealed class SpeechBubbleQueueData
        {
            /// <summary>
            ///     Time left until the next speech bubble can appear.
            /// </summary>
            public float TimeLeft { get; set; }

            public Queue<SpeechBubbleData> MessageQueue { get; } = new Queue<SpeechBubbleData>();
        }
    }
}
