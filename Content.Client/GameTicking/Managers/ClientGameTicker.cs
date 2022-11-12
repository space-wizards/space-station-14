using Content.Client.Audio;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.GameTicking.Managers
{
    [UsedImplicitly]
    public sealed class ClientGameTicker : SharedGameTicker
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        [ViewVariables] private bool _initialized;
        private Dictionary<EntityUid, Dictionary<string, uint?>>  _jobsAvailable = new();
        private Dictionary<EntityUid, string> _stationNames = new();

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string? LobbySong { get; private set; }
        [ViewVariables] public string? RestartSound { get; private set; }
        [ViewVariables] public string? LobbyBackground { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string? ServerInfoBlob { get; private set; }
        [ViewVariables] public TimeSpan StartTime { get; private set; }
        [ViewVariables] public TimeSpan RoundStartTimeSpan { get; private set; }
        [ViewVariables] public new bool Paused { get; private set; }

        [ViewVariables] public IReadOnlyDictionary<EntityUid, Dictionary<string, uint?>> JobsAvailable => _jobsAvailable;
        [ViewVariables] public IReadOnlyDictionary<EntityUid, string> StationNames => _stationNames;

        public event Action? InfoBlobUpdated;
        public event Action? LobbyStatusUpdated;
        public event Action? LobbyReadyUpdated;
        public event Action? LobbyLateJoinStatusUpdated;
        public event Action<IReadOnlyDictionary<EntityUid, Dictionary<string, uint?>>>? LobbyJobsAvailableUpdated;

        public override void Initialize()
        {
            DebugTools.Assert(!_initialized);

            SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
            SubscribeNetworkEvent<TickerJoinGameEvent>(JoinGame);
            SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
            SubscribeNetworkEvent<TickerLobbyInfoEvent>(LobbyInfo);
            SubscribeNetworkEvent<TickerLobbyCountdownEvent>(LobbyCountdown);
            SubscribeNetworkEvent<TickerLobbyReadyEvent>(LobbyReady);
            SubscribeNetworkEvent<RoundEndMessageEvent>(RoundEnd);
            SubscribeNetworkEvent<RequestWindowAttentionEvent>(msg =>
            {
                IoCManager.Resolve<IClyde>().RequestWindowAttention();
            });
            SubscribeNetworkEvent<TickerLateJoinStatusEvent>(LateJoinStatus);
            SubscribeNetworkEvent<TickerJobsAvailableEvent>(UpdateJobsAvailable);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);

            _initialized = true;
        }

        private void LateJoinStatus(TickerLateJoinStatusEvent message)
        {
            DisallowedLateJoin = message.Disallowed;
            LobbyLateJoinStatusUpdated?.Invoke();
        }

        private void UpdateJobsAvailable(TickerJobsAvailableEvent message)
        {
            _jobsAvailable = message.JobsAvailableByStation;
            _stationNames = message.StationNames;
            LobbyJobsAvailableUpdated?.Invoke(JobsAvailable);
        }

        private void JoinLobby(TickerJoinLobbyEvent message)
        {
            _stateManager.RequestStateChange<LobbyState>();
        }

        private void LobbyStatus(TickerLobbyStatusEvent message)
        {
            StartTime = message.StartTime;
            RoundStartTimeSpan = message.RoundStartTimeSpan;
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
            LobbySong = message.LobbySong;
            LobbyBackground = message.LobbyBackground;
            Paused = message.Paused;

            LobbyStatusUpdated?.Invoke();
        }

        private void LobbyInfo(TickerLobbyInfoEvent message)
        {
            ServerInfoBlob = message.TextBlob;

            InfoBlobUpdated?.Invoke();
        }

        private void JoinGame(TickerJoinGameEvent message)
        {
            _stateManager.RequestStateChange<GameplayState>();
        }

        private void LobbyCountdown(TickerLobbyCountdownEvent message)
        {
            StartTime = message.StartTime;
            Paused = message.Paused;
        }

        private void LobbyReady(TickerLobbyReadyEvent message)
        {
            LobbyReadyUpdated?.Invoke();
        }

        private void RoundEnd(RoundEndMessageEvent message)
        {
            if (message.LobbySong != null)
            {
                LobbySong = message.LobbySong;
                Get<BackgroundAudioSystem>().StartLobbyMusic();
            }

            RestartSound = message.RestartSound;

            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            var roundEnd = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText, message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, _entityManager);
        }

        private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
        {
            if (string.IsNullOrEmpty(RestartSound))
                return;

            if (!_configManager.GetCVar(CCVars.RestartSoundsEnabled))
            {
                RestartSound = null;
                return;
            }

            SoundSystem.Play(RestartSound, Filter.Empty());

            // Cleanup the sound, we only want it to play when the round restarts after it ends normally.
            RestartSound = null;
        }
    }
}
