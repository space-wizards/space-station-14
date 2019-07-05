using System;
using Content.Client.Interfaces.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Client.UserInterface.Controls;
using System.Collections.Generic;

namespace Content.Client.Chat
{
    internal sealed class ChatManager : IChatManager
    {
        private const char ConCmdSlash = '/';
        private const char OOCAlias = '[';
        private const char MeAlias = '@';

        // Holds any missed messages due to filtering for re-addition to chat later
<<<<<<< Updated upstream
        public SortedDictionary<DateTime,MsgChatMessage> filteredHistory = new SortedDictionary<DateTime,MsgChatMessage>();
=======
        public List<MsgChatMessage> filteredHistory = new List<MsgChatMessage>();
>>>>>>> Stashed changes

        // Filter Button states
        private bool _ALLstate;
        private bool _OOCstate;

        // List for holding currently filtered channels
        public List<Enum> filteredChannels = new List<Enum>();

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
                _currentChatBox.FilterPressed -= _onFilterButtonToggled;
            }

            _currentChatBox = chatBox;
            if (_currentChatBox != null)
            {
                _currentChatBox.TextSubmitted += _onChatBoxTextSubmitted;
                _currentChatBox.FilterPressed += _onFilterButtonToggled;
            }
        }

        private void _onChatMessage(MsgChatMessage message)
        {
            Logger.Debug($"{message.Channel}: {message.Message}");

<<<<<<< Updated upstream
            if (filteredHistory.ContainsValue(message))
            {
                return;
            }

            // Set time message sent
            message.TimeStamp = DateTime.Now;

            if (!IsFiltered(message))
            {


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

                // Log all incoming chat to repopulate when filter if a filter is untoggled
                filteredHistory.Add(message.TimeStamp, message);
                _currentChatBox?.AddLine(messageText, message.Channel, color, message.TimeStamp);
            }
            else
            {
                Logger.Debug($"Message filtered: {message.Channel}: {message.Message}");
                filteredHistory.Add(message.TimeStamp, message);
=======
            // Set time message sent
            message.TimeStamp = DateTime.Now;

            if (!IsFiltered(message))
            {

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

                _currentChatBox?.AddLine(messageText, message.Channel, color, message.TimeStamp);
            }
            else
            {
                filteredHistory.Add(message);
                foreach (MsgChatMessage msg in filteredHistory)
                {
                    System.Console.WriteLine(msg.Message);
                    System.Console.WriteLine(msg.TimeStamp);
                }
                foreach (var channel in filteredChannels)
                {
                    System.Console.WriteLine(channel);
                }
>>>>>>> Stashed changes
            }
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

        private void _onFilterButtonToggled(ChatBox chatBox, Button.ButtonEventArgs e)
        {
<<<<<<< Updated upstream
=======

            System.Console.WriteLine("A button was toggled");
>>>>>>> Stashed changes
            switch (e.Button.Name)
            {
                case "OOC":

                _OOCstate = !_OOCstate;
                if (_OOCstate)
                {
                    filteredChannels.Add(ChatChannel.OOC);
                    break;
<<<<<<< Updated upstream

                } else
                {
                    filteredChannels.Remove(ChatChannel.OOC);
                    _currentChatBox.contents.Clear();
                    RepopulateChat(filteredHistory);

=======
                } else
                {
                    filteredChannels.Remove(ChatChannel.OOC);
                    // TODO re-populate chatbox with missed messages matching this channel type
>>>>>>> Stashed changes
                    break;
                }

                default:

                _ALLstate = !_ALLstate;
                if (_ALLstate)
                {
                    foreach (string enumString in ChatChannel.GetNames(typeof(ChatChannel)))
                    {
                        ChatChannel.TryParse(enumString, out ChatChannel channel);
                        filteredChannels.Add(channel);
                    }
                }
                else 
                {
                    foreach (string enumString in ChatChannel.GetNames(typeof(ChatChannel)))
                    {
                        ChatChannel.TryParse(enumString, out ChatChannel channel);
                        filteredChannels.Remove(channel);
<<<<<<< Updated upstream
                        _currentChatBox.contents.Clear();
                    } 

                    RepopulateChat(filteredHistory);                   
=======
                    }                    
>>>>>>> Stashed changes
                }

                break;
            }
        }

<<<<<<< Updated upstream
        private void RepopulateChat(SortedDictionary<DateTime, MsgChatMessage> filteredMessages)
        {
            // Copy the dict for enumration
            SortedDictionary<DateTime, MsgChatMessage> filteredHistoryCopy = new SortedDictionary<DateTime, MsgChatMessage>(filteredMessages);

            foreach ( KeyValuePair<DateTime, MsgChatMessage> item in filteredHistoryCopy)
            {
                filteredMessages.Remove(item.Key);
                _onChatMessage(item.Value);        
            }

            // filteredHistoryCopy.Clear();
            // filteredMessages.Clear();
        }

=======
>>>>>>> Stashed changes
        private bool IsFiltered(MsgChatMessage message)
        {
            if (filteredChannels.Contains(message.Channel))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

<<<<<<< Updated upstream
=======
       

>>>>>>> Stashed changes
    }
}
