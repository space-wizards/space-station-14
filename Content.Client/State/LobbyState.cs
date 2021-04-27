using System;
using System.Linq;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Client.Voting;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameTicking.SharedGameTicker;

namespace Content.Client.State
{
    public class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IClientGameTicker _clientGameTicker = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;

        [ViewVariables] private CharacterSetupGui _characterSetup = default!;
        [ViewVariables] private LobbyGui _lobby = default!;

        public override void Startup()
        {
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                _userInterfaceManager.StateRoot.AddChild(_lobby);
                _userInterfaceManager.StateRoot.RemoveChild(_characterSetup);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                _lobby?.CharacterPreview.UpdateUI();
            };

            _lobby = new LobbyGui(_entityManager, _preferencesManager);
            _userInterfaceManager.StateRoot.AddChild(_lobby);

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _chatManager.SetChatBox(_lobby.Chat);
            _voteManager.SetPopupContainer(_lobby.VoteContainer);

            _lobby.Chat.DefaultChatFormat = "ooc \"{0}\"";

            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName;

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChat(_lobby.Chat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusOOC,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(_lobby.Chat, ChatChannel.OOC)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(_lobby.Chat, ChatChannel.AdminChat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
                InputCmdHandler.FromDelegate(_ => GameScreen.CycleChatChannel(_lobby.Chat, true)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
                InputCmdHandler.FromDelegate(_ => GameScreen.CycleChatChannel(_lobby.Chat, false)));

            UpdateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += _ =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(_lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

            _lobby.ObserveButton.OnPressed += _ => _consoleHost.ExecuteCommand("observe");
            _lobby.ReadyButton.OnPressed += _ =>
            {
                if (!_clientGameTicker.IsGameStarted)
                {
                    return;
                }

                new LateJoinGui().OpenCentered();
            };

            _lobby.ReadyButton.OnToggled += args =>
            {
                SetReady(args.Pressed);
            };

            _lobby.LeaveButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
            _lobby.OptionsButton.OnPressed += _ => new OptionsMenu().Open();

            UpdatePlayerList();

            _playerManager.PlayerListUpdated += PlayerManagerOnPlayerListUpdated;
            _clientGameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _clientGameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _clientGameTicker.LobbyReadyUpdated += LobbyReadyUpdated;
            _clientGameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;
        }

        public override void Shutdown()
        {
            _playerManager.PlayerListUpdated -= PlayerManagerOnPlayerListUpdated;
            _clientGameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _clientGameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _clientGameTicker.LobbyReadyUpdated -= LobbyReadyUpdated;
            _clientGameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;

            _clientGameTicker.Status.Clear();

            _lobby.Dispose();
            _characterSetup.Dispose();
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_clientGameTicker.IsGameStarted)
            {
                _lobby.StartTime.Text = "";
                return;
            }

            string text;

            if (_clientGameTicker.Paused)
            {
                text = Loc.GetString("Paused");
            }
            else
            {
                var difference = _clientGameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "Right Now?" : "Right Now");
                }
                else
                {
                    text = $"{(int) Math.Floor(difference.TotalMinutes / 60)}:{difference.Seconds:D2}";
                }
            }

            _lobby.StartTime.Text = Loc.GetString("Round Starts In: {0}", text);
        }

        private void PlayerManagerOnPlayerListUpdated(object? sender, EventArgs e)
        {
            // Remove disconnected sessions from the Ready Dict
            foreach (var p in _clientGameTicker.Status)
            {
                if (!_playerManager.SessionsDict.TryGetValue(p.Key, out _))
                {
                    // This is a shitty fix. Observers can rejoin because they are already in the game.
                    // So we don't delete them, but keep them if they decide to rejoin
                    if (p.Value != PlayerStatus.Observer)
                        _clientGameTicker.Status.Remove(p.Key);
                }
            }

            UpdatePlayerList();
        }

        private void LobbyReadyUpdated() => UpdatePlayerList();

        private void LobbyStatusUpdated()
        {
            UpdatePlayerList();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            _lobby.ReadyButton.Disabled = _clientGameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_lobby == null)
            {
                return;
            }

            if (_clientGameTicker.IsGameStarted)
            {
                _lobby.ReadyButton.Text = Loc.GetString("Join");
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
            }
            else
            {
                _lobby.StartTime.Text = "";
                _lobby.ReadyButton.Text = Loc.GetString("Ready Up");
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Disabled = false;
                _lobby.ReadyButton.Pressed = _clientGameTicker.AreWeReady;
            }

            if (_clientGameTicker.ServerInfoBlob != null)
            {
                _lobby.ServerInfo.SetInfoBlob(_clientGameTicker.ServerInfoBlob);
            }
        }

        private void UpdatePlayerList()
        {
            _lobby.OnlinePlayerList.Clear();

            foreach (var session in _playerManager.Sessions.OrderBy(s => s.Name))
            {
                var readyState = "";
                // Don't show ready state if we're ingame
                if (!_clientGameTicker.IsGameStarted)
                {
                    PlayerStatus status;
                    if (session.UserId == _playerManager.LocalPlayer?.UserId)
                        status = _clientGameTicker.AreWeReady ? PlayerStatus.Ready : PlayerStatus.NotReady;
                    else
                        _clientGameTicker.Status.TryGetValue(session.UserId, out status);

                    readyState = status switch
                    {
                        PlayerStatus.NotReady => Loc.GetString("Not Ready"),
                        PlayerStatus.Ready => Loc.GetString("Ready"),
                        PlayerStatus.Observer => Loc.GetString("Observer"),
                        _ => "",
                    };
                }

                _lobby.OnlinePlayerList.AddItem(session.Name, readyState);
            }
        }

        private void SetReady(bool newReady)
        {
            if (_clientGameTicker.IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
            UpdatePlayerList();
        }
    }
}
