using System.Collections.Generic;
using Content.Client.Interfaces.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Chat
{
    internal sealed class ChatManager : IChatManager
    {
        private const char ConCmdSlash = '/';
        private const char OOCAlias = '[';
        private const char MeAlias = '@';

        public List<StoredChatMessage> filteredHistory = new List<StoredChatMessage>();

        // Filter Button States
        private bool _allState;
        private bool _localState;
        private bool _oocState;

        // Flag Enums for holding filtered channels
        private ChatChannel _filteredChannels;

#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly IClientConsole _console;
#pragma warning restore 649

        private ChatBox _currentChatBox;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgChatMessage>(MsgChatMessage.NAME, _onChatMessage);
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
            _currentChatBox.contents.Clear();

            foreach (var msg in filteredMessages)
            {
                WriteChatMessage(msg);
            }
        }

        private void _onChatMessage(MsgChatMessage msg)
        {
            Logger.Debug($"{msg.Channel}: {msg.Message}");

            // Log all incoming chat to repopulate when filter is un-toggled
            StoredChatMessage storedMessage = new StoredChatMessage(msg);
            filteredHistory.Add(storedMessage);
            WriteChatMessage(storedMessage);
        }

        private bool IsFiltered(ChatChannel channel)
        {
            // _ALLstate works as inverter.
            return _allState ^ _filteredChannels.HasFlag(channel);
        }
    }
}
