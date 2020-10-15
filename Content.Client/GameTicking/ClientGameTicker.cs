using System;
using System.Collections.Generic;
using Content.Client.Interfaces;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Shared;
using Content.Shared.GameTicking;
using Content.Shared.Network.NetMessages;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.State;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameTicking
{
    public class ClientGameTicker : SharedGameTicker, IClientGameTicker
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;

        [ViewVariables] private bool _initialized;

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string ServerInfoBlob { get; private set; }
        [ViewVariables] public DateTime StartTime { get; private set; }
        [ViewVariables] public bool Paused { get; private set; }
        [ViewVariables] public Dictionary<NetUserId, PlayerStatus> Status { get; private set; }

        public event Action InfoBlobUpdated;
        public event Action LobbyStatusUpdated;
        public event Action LobbyReadyUpdated;
        public event Action LobbyLateJoinStatusUpdated;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby), JoinLobby);
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame), JoinGame);
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus), LobbyStatus);
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo), LobbyInfo);
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown), LobbyCountdown);
            _netManager.RegisterNetMessage<MsgTickerLobbyReady>(nameof(MsgTickerLobbyReady), LobbyReady);
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage), RoundEnd);
            _netManager.RegisterNetMessage<MsgRequestWindowAttention>(nameof(MsgRequestWindowAttention), msg =>
            {
                IoCManager.Resolve<IClyde>().RequestWindowAttention();
            });
            _netManager.RegisterNetMessage<MsgTickerLateJoinStatus>(nameof(MsgTickerLateJoinStatus), LateJoinStatus);

            Status = new Dictionary<NetUserId, PlayerStatus>();
            _initialized = true;
        }

        private void LateJoinStatus(MsgTickerLateJoinStatus message)
        {
            DisallowedLateJoin = message.Disallowed;
            LobbyLateJoinStatusUpdated?.Invoke();
        }


        private void JoinLobby(MsgTickerJoinLobby message)
        {
            _stateManager.RequestStateChange<LobbyState>();
        }

        private void LobbyStatus(MsgTickerLobbyStatus message)
        {
            StartTime = message.StartTime;
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
            Paused = message.Paused;
            if (IsGameStarted)
                Status.Clear();

            LobbyStatusUpdated?.Invoke();
        }

        private void LobbyInfo(MsgTickerLobbyInfo message)
        {
            ServerInfoBlob = message.TextBlob;

            InfoBlobUpdated?.Invoke();
        }

        private void JoinGame(MsgTickerJoinGame message)
        {
            _stateManager.RequestStateChange<GameScreen>();
        }

        private void LobbyCountdown(MsgTickerLobbyCountdown message)
        {
            StartTime = message.StartTime;
            Paused = message.Paused;
        }

        private void LobbyReady(MsgTickerLobbyReady message)
        {
            // Merge the Dictionaries
            foreach (var p in message.PlayerStatus)
            {
                Status[p.Key] = p.Value;
            }
            LobbyReadyUpdated?.Invoke();
        }

        private void RoundEnd(MsgRoundEndMessage message)
        {
            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            var roundEnd = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText, message.RoundDuration, message.AllPlayersEndInfo);

        }
    }
}
