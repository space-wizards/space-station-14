using System;
using System.Collections.Generic;
using System.Net;
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
        private bool _ALLstate;
        private bool _Localstate;
        private bool _OOCstate;

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

        private void _onFilterButtonToggled(ChatBox chatBox, Button.ButtonToggledEventArgs e)
        {
            // TODO make toggled ALL button flip all button states programatically + visually
            switch (e.Button.Name)
            {
                case "Local":
                    _Localstate = !_Localstate;
                    if (_Localstate)
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
                    _OOCstate = !_OOCstate;
                    if (_OOCstate)
                    {
                        _filteredChannels |= ChatChannel.OOC;
                        break;
                    }
                    else
                    {
                        _filteredChannels &= ~ChatChannel.OOC;
                        break;
                    }

                default:
                    _ALLstate = !_ALLstate;
                    if (_ALLstate)
                    {
                        _filteredChannels = ChatChannel.OOC | ChatChannel.Local;
                        break;
                    }
                    else
                    {
                        _filteredChannels &= ~ChatChannel.OOC;
                        _filteredChannels &= ~ChatChannel.Local;
                        break;
                    }
            }

            RepopulateChat(filteredHistory);
        }

        private void RepopulateChat(List<StoredChatMessage> filteredMessages)
        {
            _currentChatBox.contents.Clear();

            // Copy list for enumeration
            List<StoredChatMessage> filteredMessagesCopy = new List<StoredChatMessage>(filteredMessages);

            foreach (StoredChatMessage msg in filteredMessagesCopy)
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
            if (_filteredChannels.HasFlag(channel))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
