using System;
using Content.Client.Chat;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Shared;
using Content.Shared.Input;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameTicking
{
    public class ClientGameTicker : SharedGameTicker, IClientGameTicker
    {
#pragma warning disable 649
        [Dependency] private IClientNetManager _netManager;
        [Dependency] private IUserInterfaceManager _userInterfaceManager;
        [Dependency] private IInputManager _inputManager;
        [Dependency] private IBaseClient _baseClient;
        [Dependency] private IChatManager _chatManager;
        [Dependency] private IClientConsole _console;
#pragma warning restore 649

        [ViewVariables] private bool _areWeReady;
        [ViewVariables] private bool _initialized;
        [ViewVariables] private TickerState _tickerState;
        [ViewVariables] private ChatBox _gameChat;
        [ViewVariables] private LobbyGui _lobby;
        [ViewVariables] private bool _gameStarted;
        [ViewVariables] private DateTime _startTime;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby), _joinLobby);
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame), _joinGame);
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus), _lobbyStatus);

            _baseClient.RunLevelChanged += BaseClientOnRunLevelChanged;

            _initialized = true;
        }

        private void BaseClientOnRunLevelChanged(object sender, RunLevelChangedEventArgs e)
        {
            if (e.NewLevel != ClientRunLevel.Initialize)
            {
                return;
            }

            _tickerState = TickerState.Unset;
            _lobby?.Dispose();
            _lobby = null;
            _gameChat?.Dispose();
            _gameChat = null;
        }

        public void FrameUpdate(RenderFrameEventArgs renderFrameEventArgs)
        {
            if (_lobby == null)
            {
                return;
            }

            if (_gameStarted)
            {
                _lobby.StartTime.Text = "";
                return;
            }

            string text;
            var difference = _startTime - DateTime.UtcNow;
            if (difference.Ticks < 0)
            {
                if (difference.TotalSeconds < -5)
                {
                    text = "Right Now?";
                }
                else
                {
                    text = "Right Now";
                }
            }
            else
            {
                text = $"{(int) Math.Floor(difference.TotalMinutes)}:{difference.Seconds:D2}";
            }

            _lobby.StartTime.Text = "Round Starts In: " + text;
        }

        private void _lobbyStatus(MsgTickerLobbyStatus message)
        {
            _startTime = message.StartTime;
            _gameStarted = message.IsRoundStarted;
            _areWeReady = message.YouAreReady;

            _updateLobbyUi();
        }

        private void _updateLobbyUi()
        {
            if (_lobby == null)
            {
                return;
            }

            if (_gameStarted)
            {
                _lobby.ReadyButton.Text = "Join";
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
            }
            else
            {
                _lobby.StartTime.Text = "";
                _lobby.ReadyButton.Text = "Ready Up";
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Pressed = _areWeReady;
            }
        }

        private void _joinLobby(MsgTickerJoinLobby message)
        {
            if (_tickerState == TickerState.InLobby)
            {
                return;
            }

            if (_gameChat != null)
            {
                _gameChat.Dispose();
                _gameChat = null;
            }

            _tickerState = TickerState.InLobby;

            _lobby = new LobbyGui();
            _userInterfaceManager.StateRoot.AddChild(_lobby);

            _chatManager.SetChatBox(_lobby.Chat);
            _lobby.Chat.DefaultChatFormat = "ooc \"{0}\"";

            _lobby.ServerName.Text = _baseClient.GameInfo.ServerName;

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(session =>
                {
                    _lobby.Chat.Input.IgnoreNext = true;
                    _lobby.Chat.Input.GrabKeyboardFocus();
                }));

            _updateLobbyUi();

            _lobby.ObserveButton.OnPressed += args => _console.ProcessCommand("observe");
            _lobby.ReadyButton.OnPressed += args =>
            {
                if (!_gameStarted)
                {
                    return;
                }

                _console.ProcessCommand("joingame");
            };

            _lobby.ReadyButton.OnToggled += args =>
            {
                if (_gameStarted)
                {
                    return;
                }

                _console.ProcessCommand($"toggleready {args.Pressed}");
            };

            _lobby.LeaveButton.OnPressed += args => _console.ProcessCommand("disconnect");
        }

        private void _joinGame(MsgTickerJoinGame message)
        {
            if (_tickerState == TickerState.InGame)
            {
                return;
            }

            _tickerState = TickerState.InGame;

            if (_lobby != null)
            {
                _lobby.Dispose();
                _lobby = null;
            }

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(session =>
                {
                    _lobby.Chat.Input.IgnoreNext = true;
                    _gameChat.Input.GrabKeyboardFocus();
                }));

            _gameChat = new ChatBox();
            _userInterfaceManager.StateRoot.AddChild(_gameChat);
            _chatManager.SetChatBox(_gameChat);
            _gameChat.DefaultChatFormat = "say \"{0}\"";
        }

        private enum TickerState
        {
            Unset = 0,

            /// <summary>
            ///     The client is in the lobby.
            /// </summary>
            InLobby = 1,

            /// <summary>
            ///     The client is NOT in the lobby.
            ///     Do not confuse this with the client session status.
            /// </summary>
            InGame = 2
        }
    }
}
