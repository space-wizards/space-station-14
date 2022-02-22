using System;
using System.Collections.Generic;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Client.Viewport;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameTicking.Managers
{
    [UsedImplicitly]
    public sealed class ClientGameTicker : SharedGameTicker
    {
        [Dependency] private readonly IStateManager _stateManager = default!;

        [ViewVariables] private bool _initialized;
        private Dictionary<StationId, Dictionary<string, int>>  _jobsAvailable = new();
        private Dictionary<StationId, string> _stationNames = new();

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string? LobbySong { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string? ServerInfoBlob { get; private set; }
        [ViewVariables] public TimeSpan StartTime { get; private set; }
        [ViewVariables] public new bool Paused { get; private set; }
        [ViewVariables] public Dictionary<NetUserId, LobbyPlayerStatus> Status { get; private set; } = new();
        [ViewVariables] public IReadOnlyDictionary<StationId, Dictionary<string, int>> JobsAvailable => _jobsAvailable;
        [ViewVariables] public IReadOnlyDictionary<StationId, string> StationNames => _stationNames;

        public event Action? InfoBlobUpdated;
        public event Action? LobbyStatusUpdated;
        public event Action? LobbyReadyUpdated;
        public event Action? LobbyLateJoinStatusUpdated;
        public event Action<IReadOnlyDictionary<StationId, Dictionary<string, int>>>? LobbyJobsAvailableUpdated;

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

            Status = new Dictionary<NetUserId, LobbyPlayerStatus>();
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
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
            LobbySong = message.LobbySong;
            Paused = message.Paused;
            if (IsGameStarted)
                Status.Clear();

            LobbyStatusUpdated?.Invoke();
        }

        private void LobbyInfo(TickerLobbyInfoEvent message)
        {
            ServerInfoBlob = message.TextBlob;

            InfoBlobUpdated?.Invoke();
        }

        private void JoinGame(TickerJoinGameEvent message)
        {
            _stateManager.RequestStateChange<GameScreen>();
        }

        private void LobbyCountdown(TickerLobbyCountdownEvent message)
        {
            StartTime = message.StartTime;
            Paused = message.Paused;
        }

        private void LobbyReady(TickerLobbyReadyEvent message)
        {
            // Merge the Dictionaries
            foreach (var p in message.Status)
            {
                Status[p.Key] = p.Value;
            }
            LobbyReadyUpdated?.Invoke();
        }

        private void RoundEnd(RoundEndMessageEvent message)
        {
            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            var roundEnd = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText, message.RoundDuration, message.AllPlayersEndInfo);
        }
    }
}
