using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Utility;

namespace Content.Client.GameTicking.Managers
{
    [UsedImplicitly]
    public sealed class ClientGameTicker : SharedGameTicker
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private bool _initialized;
        private Dictionary<NetEntity, Dictionary<string, uint?>>  _jobsAvailable = new();
        private Dictionary<NetEntity, string> _stationNames = new();

        /// <summary>
        /// The current round-end window. Could be used to support re-opening the window after closing it.
        /// </summary>
        private RoundEndSummaryWindow? _window;

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string? RestartSound { get; private set; }
        [ViewVariables] public string? LobbyBackground { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string? ServerInfoBlob { get; private set; }
        [ViewVariables] public TimeSpan StartTime { get; private set; }
        [ViewVariables] public new bool Paused { get; private set; }

        [ViewVariables] public IReadOnlyDictionary<NetEntity, Dictionary<string, uint?>> JobsAvailable => _jobsAvailable;
        [ViewVariables] public IReadOnlyDictionary<NetEntity, string> StationNames => _stationNames;

        public event Action? InfoBlobUpdated;
        public event Action? LobbyStatusUpdated;
        public event Action? LobbyLateJoinStatusUpdated;
        public event Action<IReadOnlyDictionary<NetEntity, Dictionary<string, uint?>>>? LobbyJobsAvailableUpdated;

        public override void Initialize()
        {
            DebugTools.Assert(!_initialized);

            SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
            SubscribeNetworkEvent<TickerJoinGameEvent>(JoinGame);
            SubscribeNetworkEvent<TickerConnectionStatusEvent>(ConnectionStatus);
            SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
            SubscribeNetworkEvent<TickerLobbyInfoEvent>(LobbyInfo);
            SubscribeNetworkEvent<TickerLobbyCountdownEvent>(LobbyCountdown);
            SubscribeNetworkEvent<RoundEndMessageEvent>(RoundEnd);
            SubscribeNetworkEvent<RequestWindowAttentionEvent>(msg =>
            {
                IoCManager.Resolve<IClyde>().RequestWindowAttention();
            });
            SubscribeNetworkEvent<TickerLateJoinStatusEvent>(LateJoinStatus);
            SubscribeNetworkEvent<TickerJobsAvailableEvent>(UpdateJobsAvailable);

            _initialized = true;
        }

        private void LateJoinStatus(TickerLateJoinStatusEvent message)
        {
            DisallowedLateJoin = message.Disallowed;
            LobbyLateJoinStatusUpdated?.Invoke();
        }

        private void UpdateJobsAvailable(TickerJobsAvailableEvent message)
        {
            _jobsAvailable.Clear();

            foreach (var (job, data) in message.JobsAvailableByStation)
            {
                _jobsAvailable[job] = data;
            }

            _stationNames.Clear();
            foreach (var weh in message.StationNames)
            {
                _stationNames[weh.Key] = weh.Value;
            }

            LobbyJobsAvailableUpdated?.Invoke(JobsAvailable);
        }

        private void JoinLobby(TickerJoinLobbyEvent message)
        {
            _stateManager.RequestStateChange<LobbyState>();
        }

        private void ConnectionStatus(TickerConnectionStatusEvent message)
        {
            RoundStartTimeSpan = message.RoundStartTimeSpan;
        }

        private void LobbyStatus(TickerLobbyStatusEvent message)
        {
            StartTime = message.StartTime;
            RoundStartTimeSpan = message.RoundStartTimeSpan;
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
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

        private void RoundEnd(RoundEndMessageEvent message)
        {
            // Force an update in the event of this song being the same as the last.
            RestartSound = message.RestartSound;

            // Don't open duplicate windows (mainly for replays).
            if (_window?.RoundId == message.RoundId)
                return;

            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText, message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, _entityManager);
        }
    }
}
