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
        private LobbyGui? _lobby;

        protected override void Startup()
        {
            if (_userInterfaceManager.ActiveScreen == null)
            {
                return;
            }

            _lobby = (LobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            _gameTicker = _entityManager.System<ClientGameTicker>();
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager, _configurationManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _lobby.CharacterSetupState.AddChild(_characterSetup);
            chatController.SetMainChat(true);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                _lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                _lobby.CharacterPreview.UpdateUI();
            };

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);
            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; //The eye of refactor gazes upon you...
            UpdateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += OnSetupPressed;
            _lobby.ReadyButton.OnPressed += OnReadyPressed;
            _lobby.ReadyButton.OnToggled += OnReadyToggled;

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

            _lobby!.CharacterPreview.CharacterSetupButton.OnPressed -= OnSetupPressed;
            _lobby!.ReadyButton.OnPressed -= OnReadyPressed;
            _lobby!.ReadyButton.OnToggled -= OnReadyToggled;

            _lobby = null;

            _characterSetup?.Dispose();
            _characterSetup = null;
        }

        private void OnSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            _lobby!.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void OnReadyPressed(BaseButton.ButtonEventArgs args)
        {
            if (!_gameTicker.IsGameStarted)
            {
                return;
            }

            new LateJoinGui().OpenCentered();
        }

        private void OnReadyToggled(BaseButton.ButtonToggledEventArgs args)
        {
            SetReady(args.Pressed);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                _lobby!.StartTime.Text = string.Empty;
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                _lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time", ("hours", roundTime.Hours), ("minutes", roundTime.Minutes));
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

            _lobby!.StationTime.Text =  Loc.GetString("lobby-state-player-status-round-not-started");
            _lobby!.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyBackground();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            _lobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                _lobby!.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                _lobby!.ReadyButton.ToggleMode = false;
                _lobby!.ReadyButton.Pressed = false;
                _lobby!.ObserveButton.Disabled = false;
            }
            else
            {
                _lobby!.StartTime.Text = string.Empty;
                _lobby!.ReadyButton.Text = Loc.GetString(_lobby!.ReadyButton.Pressed ? "lobby-state-player-status-ready": "lobby-state-player-status-not-ready");
                _lobby!.ReadyButton.ToggleMode = true;
                _lobby!.ReadyButton.Disabled = false;
                _lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                _lobby!.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                _lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }
        }

        private void UpdateLobbyBackground()
        {
            if (_gameTicker.LobbyBackground != null)
            {
                _lobby!.Background.Texture = _resourceCache.GetResource<TextureResource>(_gameTicker.LobbyBackground );
            }
            else
            {
                _lobby!.Background.Texture = null;
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
