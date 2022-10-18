using Content.Client.Chat.Managers;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Client.UserInterface.Systems.EscapeMenu;


namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        [ViewVariables] private CharacterSetupGui? _characterSetup;

        private ClientGameTicker _gameTicker = default!;

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);
        private LobbyGui Lobby => (LobbyGui) _userInterfaceManager.ActiveScreen!;

        protected override void Startup()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            _gameTicker = _entityManager.System<ClientGameTicker>();
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager, _configurationManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            chatController.SetMainChat(true);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                _userInterfaceManager.StateRoot.AddChild(Lobby);
                _userInterfaceManager.StateRoot.RemoveChild(_characterSetup);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                Lobby?.CharacterPreview.UpdateUI();
            };

            LayoutContainer.SetAnchorPreset(Lobby, LayoutContainer.LayoutPreset.Wide);
            _voteManager.SetPopupContainer(Lobby.VoteContainer);
            Lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; //The eye of refactor gazes upon you...
            UpdateLobbyUi();

            Lobby.CharacterPreview.CharacterSetupButton.OnPressed += _ =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(Lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

            Lobby.ReadyButton.OnPressed += _ =>
            {
                if (!_gameTicker.IsGameStarted)
                {
                    return;
                }

                new LateJoinGui().OpenCentered();
            };

            Lobby.ReadyButton.OnToggled += args =>
            {
                SetReady(args.Pressed);
            };

            Lobby.LeaveButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
            Lobby.OptionsButton.OnPressed += _ => _userInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();


            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;
        }

        protected override void Shutdown()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            chatController.SetMainChat(false);
            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;

            _characterSetup?.Dispose();
            _characterSetup = null;
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                Lobby.StartTime.Text = string.Empty;
                Lobby.StationTime.Text = Loc.GetString("lobby-state-player-status-station-time", ("stationTime", _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan).ToString("hh\\:mm")));
                return;
            }

            string text;

            if (_gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
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

            Lobby.StationTime.Text =  Loc.GetString("lobby-state-player-status-station-time", ("stationTime", TimeSpan.Zero.ToString("hh\\:mm")));
            Lobby.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyBackground();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            Lobby.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                Lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                Lobby.ReadyButton.ToggleMode = false;
                Lobby.ReadyButton.Pressed = false;
                Lobby.ObserveButton.Disabled = false;
            }
            else
            {
                Lobby.StartTime.Text = string.Empty;
                Lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-ready-up-state");
                Lobby.ReadyButton.ToggleMode = true;
                Lobby.ReadyButton.Disabled = false;
                Lobby.ReadyButton.Pressed = _gameTicker.AreWeReady;
                Lobby.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                Lobby.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }
        }

        private void UpdateLobbyBackground()
        {
            if (_gameTicker.LobbyBackground != null)
            {
                Lobby.Background.Texture = _resourceCache.GetResource<TextureResource>(_gameTicker.LobbyBackground );
            }
            else
            {
                Lobby.Background.Texture = null;
            }

        }

        private void SetReady(bool newReady)
        {
            if (_gameTicker.IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }
    }
}
