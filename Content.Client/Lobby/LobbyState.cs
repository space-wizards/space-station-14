using System;
using System.Linq;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.EscapeMenu.UI;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.Resources;
using Content.Client.Voting;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        [ViewVariables] private CharacterSetupGui? _characterSetup;
        [ViewVariables] private LobbyGui? _lobby;

        private ClientGameTicker _gameTicker = default!;

        public override void Startup()
        {
            _gameTicker = EntitySystem.Get<ClientGameTicker>();
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager, _configurationManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _lobby = new LobbyGui(_entityManager, _preferencesManager);
            _userInterfaceManager.StateRoot.AddChild(_lobby);

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

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _chatManager.SetChatBox(_lobby.Chat);
            _voteManager.SetPopupContainer(_lobby.VoteContainer);

            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName;

            ChatInput.SetupChatInputHandlers(_inputManager, _lobby.Chat);

            UpdateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += _ =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(_lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

            _lobby.ReadyButton.OnPressed += _ =>
            {
                if (!_gameTicker.IsGameStarted)
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


            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;
        }

        public override void Shutdown()
        {
            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;

            _lobby?.Dispose();
            _characterSetup?.Dispose();
            _lobby = null;
            _characterSetup = null;
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_lobby == null) return;

            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            if (gameTicker.IsGameStarted)
            {
                _lobby.StartTime.Text = string.Empty;
                _lobby.StationTime.Text = Loc.GetString("lobby-state-player-status-station-time", ("stationTime", _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan).ToString("hh\\:mm")));
                return;
            }

            string text;

            if (gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else
            {
                var difference = gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "lobby-state-right-now-question" : "lobby-state-right-now-confirmation");
                }
                else
                {
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
                }
            }

            _lobby.StationTime.Text =  Loc.GetString("lobby-state-player-status-station-time", ("stationTime", TimeSpan.Zero.ToString("hh\\:mm")));
            _lobby.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyBackground();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            if (_lobby == null) return;
            _lobby.ReadyButton.Disabled = EntitySystem.Get<ClientGameTicker>().DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_lobby == null) return;

            var gameTicker = EntitySystem.Get<ClientGameTicker>();

            if (gameTicker.IsGameStarted)
            {
                _lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
                _lobby.ObserveButton.Disabled = false;
            }
            else
            {
                _lobby.StartTime.Text = string.Empty;
                _lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-ready-up-state");
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Disabled = false;
                _lobby.ReadyButton.Pressed = gameTicker.AreWeReady;
                _lobby.ObserveButton.Disabled = true;
            }

            if (gameTicker.ServerInfoBlob != null)
            {
                _lobby.ServerInfo.SetInfoBlob(gameTicker.ServerInfoBlob);
            }
        }

        private void UpdateLobbyBackground()
        {
            if (_lobby == null) return;
            if (_gameTicker.LobbyBackground != null)
            {
                _lobby.Background.Texture = _resourceCache.GetResource<TextureResource>(_gameTicker.LobbyBackground );
            }
            else
            {
                _lobby.Background.Texture = null;
            }

        }

        private void SetReady(bool newReady)
        {
            if (EntitySystem.Get<ClientGameTicker>().IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }
    }
}
