using System;
using Content.Client.Interfaces.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    internal sealed class ChatManager : IChatManager
    {
        private const char ConCmdSlash = '/';
        private const char OOCAlias = '[';
        private const char MeAlias = '@';

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
            }

            _currentChatBox = chatBox;
            if (_currentChatBox != null)
            {
                _currentChatBox.TextSubmitted += _onChatBoxTextSubmitted;
            }
        }

        private void _onChatMessage(MsgChatMessage message)
        {
            Logger.Debug($"{message.Channel}: {message.Message}");

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
    }
}
