using System;
using Content.Client.Interfaces;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Shared;
using Robust.Client.Interfaces.State;
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
        [Dependency] private IStateManager _stateManager;
#pragma warning restore 649

        [ViewVariables] private bool _initialized;

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string ServerInfoBlob { get; private set; }
        [ViewVariables] public DateTime StartTime { get; private set; }
        [ViewVariables] public bool Paused { get; private set; }

        public event Action InfoBlobUpdated;
        public event Action LobbyStatusUpdated;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby), JoinLobby);
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame), JoinGame);
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus), LobbyStatus);
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo), LobbyInfo);
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown), LobbyCountdown);
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage), RoundEnd);

            _initialized = true;
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

        private void RoundEnd(MsgRoundEndMessage message)
        {

            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            var roundEnd = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundDuration, message.AllPlayersEndInfo);

        }
    }
}
