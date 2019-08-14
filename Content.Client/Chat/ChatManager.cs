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
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    internal sealed class ChatManager : IChatManager
    {
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

        private const char ConCmdSlash = '/';
        private const char OOCAlias = '[';
        private const char MeAlias = '@';

        private readonly List<StoredChatMessage> filteredHistory = new List<StoredChatMessage>();

        // Filter Button States
        private bool _allState;
        private bool _localState;
        private bool _oocState;

        // Flag Enums for holding filtered channels
        private ChatChannel _filteredChannels;

#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly IClientConsole _console;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

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

            _speechBubbleRoot = new Control
            {
                MouseFilter = Control.MouseFilterMode.Ignore
            };
            _speechBubbleRoot.SetAnchorPreset(Control.LayoutPreset.Wide);
            _userInterfaceManager.StateRoot.AddChild(_speechBubbleRoot);
            _speechBubbleRoot.SetPositionFirst();
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

                queueData.TimeLeft += BubbleDelayBase + msg.Length * BubbleDelayFactor;

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
            }

            _currentChatBox?.AddLine(messageText, message.Channel, color);
        }

        private void _onChatBoxTextSubmitted(ChatBox chatBox, string text)
        {
            DebugTools.Assert(chatBox == _currentChatBox);

            if (string.IsNullOrWhiteSpace(text))
                return;

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
                    _console.ProcessCommand($"ooc \"{conInput}\"");
                    break;
                }
                case MeAlias:
                {
                    var conInput = text.Substring(1);
                    _console.ProcessCommand($"me \"{conInput}\"");
                    break;
                }
                default:
                {
                    var conInput = _currentChatBox.DefaultChatFormat != null
                        ? string.Format(_currentChatBox.DefaultChatFormat, text)
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

                case "ALL":
                    chatBox.LocalButton.Pressed ^= true;
                    chatBox.OOCButton.Pressed ^= true;
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
            Logger.Debug($"{msg.Channel}: {msg.Message}");

            // Log all incoming chat to repopulate when filter is un-toggled
            var storedMessage = new StoredChatMessage(msg);
            filteredHistory.Add(storedMessage);
            WriteChatMessage(storedMessage);

            // Local messages that have an entity attached get a speech bubble.
            if (msg.Channel == ChatChannel.Local && msg.SenderEntity != default)
            {
                AddSpeechBubble(msg);
            }
        }

        private void AddSpeechBubble(MsgChatMessage msg)
        {
            if (!_entityManager.TryGetEntity(msg.SenderEntity, out var entity))
            {
                Logger.WarningS("chat", "Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
                return;
            }

            // Split message into words separated by spaces.
            var words = msg.Message.Split(' ');
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

            foreach (var message in messages)
            {
                EnqueueSpeechBubble(entity, message);
            }
        }

        private void EnqueueSpeechBubble(IEntity entity, string contents)
        {
            if (!_queuedSpeechBubbles.TryGetValue(entity.Uid, out var queueData))
            {
                queueData = new SpeechBubbleQueueData();
                _queuedSpeechBubbles.Add(entity.Uid, queueData);
            }

            queueData.MessageQueue.Enqueue(contents);
        }

        private void CreateSpeechBubble(IEntity entity, string contents)
        {
            var bubble = new SpeechBubble(contents, entity, _eyeManager, this);

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

            public Queue<string> MessageQueue { get; } = new Queue<string>();
        }
    }
}
