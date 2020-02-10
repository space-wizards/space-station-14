using System;
using System.Linq;
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
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
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
        [Dependency] private ILocalizationManager _localization;
        [Dependency] private IResourceCache _resourceCache;
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IGameHud _gameHud;
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IClientPreferencesManager _preferencesManager;
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        [ViewVariables] private bool _areWeReady;
        [ViewVariables] private CharacterSetupGui _characterSetup;
        [ViewVariables] private ChatBox _gameChat;
        [ViewVariables] private bool _gameStarted;
        [ViewVariables] private bool _initialized;
        [ViewVariables] private LobbyGui _lobby;
        [ViewVariables] private string _serverInfoBlob;
        [ViewVariables] private DateTime _startTime;
        [ViewVariables] private TickerState _tickerState;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby), _joinLobby);
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame), _joinGame);
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus), _lobbyStatus);
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo), _lobbyInfo);

            _baseClient.RunLevelChanged += BaseClientOnRunLevelChanged;
            _playerManager.PlayerListUpdated += PlayerManagerOnPlayerListUpdated;

            _initialized = true;
        }

        private void PlayerManagerOnPlayerListUpdated(object sender, EventArgs e)
        {
            if (_lobby == null)
            {
                return;
            }

            _updatePlayerList();
        }

        private void _updatePlayerList()
        {
            _lobby.OnlinePlayerItemList.Clear();
            foreach (var session in _playerManager.Sessions.OrderBy(s => s.Name))
            {
                _lobby.OnlinePlayerItemList.AddItem(session.Name);
            }
        }

        private void BaseClientOnRunLevelChanged(object sender, RunLevelChangedEventArgs e)
        {
            if (e.NewLevel != ClientRunLevel.Initialize)
            {
                _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat, null);
                return;
            }

            _tickerState = TickerState.Unset;
            _lobby?.Dispose();
            _lobby = null;
            _gameChat?.Dispose();
            _gameChat = null;
            _gameHud.RootControl.Orphan();
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
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
                    text = _localization.GetString("Right Now?");
                }
                else
                {
                    text = _localization.GetString("Right Now");
                }
            }
            else
            {
                text = $"{(int) Math.Floor(difference.TotalMinutes)}:{difference.Seconds:D2}";
            }

            _lobby.StartTime.Text = _localization.GetString("Round Starts In: {0}", text);
        }

        private void _lobbyStatus(MsgTickerLobbyStatus message)
        {
            _startTime = message.StartTime;
            _gameStarted = message.IsRoundStarted;
            _areWeReady = message.YouAreReady;

            _updateLobbyUi();
        }

        private void _lobbyInfo(MsgTickerLobbyInfo message)
        {
            _serverInfoBlob = message.TextBlob;

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
                _lobby.ReadyButton.Text = _localization.GetString("Join");
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
            }
            else
            {
                _lobby.StartTime.Text = "";
                _lobby.ReadyButton.Text = _localization.GetString("Ready Up");
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Pressed = _areWeReady;
            }

            _lobby.ServerInfo.SetInfoBlob(_serverInfoBlob);
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

            _gameHud.RootControl.Orphan();

            _tickerState = TickerState.InLobby;

            _characterSetup = new CharacterSetupGui(_entityManager, _localization, _resourceCache, _preferencesManager, _prototypeManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);
            _characterSetup.CloseButton.OnPressed += args =>
            {
                _characterSetup.Save();
                _lobby.CharacterPreview.UpdateUI();
                _userInterfaceManager.StateRoot.AddChild(_lobby);
                _userInterfaceManager.StateRoot.RemoveChild(_characterSetup);
            };
            _lobby = new LobbyGui(_entityManager, _localization, _resourceCache, _preferencesManager);
            _userInterfaceManager.StateRoot.AddChild(_lobby);

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _chatManager.SetChatBox(_lobby.Chat);
            _lobby.Chat.DefaultChatFormat = "ooc \"{0}\"";

            _lobby.ServerName.Text = _baseClient.GameInfo.ServerName;

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(s => _focusChat(_lobby.Chat)));

            _updateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += args =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(_lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

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
                SetReady(args.Pressed);
            };

            _lobby.LeaveButton.OnPressed += args => _console.ProcessCommand("disconnect");

            _updatePlayerList();
        }

        private void SetReady(bool newReady)
        {
            if (_gameStarted)
            {
                return;
            }

            _console.ProcessCommand($"toggleready {newReady}");
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

            _gameChat = new ChatBox();
            _userInterfaceManager.StateRoot.AddChild(_gameChat);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginLeft(_gameChat, -475);
            LayoutContainer.SetMarginBottom(_gameChat, 235);

            _userInterfaceManager.StateRoot.AddChild(_gameHud.RootControl);
            _chatManager.SetChatBox(_gameChat);
            _gameChat.DefaultChatFormat = "say \"{0}\"";
            _gameChat.Input.PlaceHolder = _localization.GetString("Say something! [ for OOC");

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(s => _focusChat(_gameChat)));
        }

        private void _focusChat(ChatBox chat)
        {
            if (chat == null || _userInterfaceManager.KeyboardFocused != null)
            {
                return;
            }

            chat.Input.IgnoreNext = true;
            chat.Input.GrabKeyboardFocus();
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
