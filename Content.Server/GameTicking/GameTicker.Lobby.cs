using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        [ViewVariables]
        private readonly Dictionary<IPlayerSession, PlayerStatus> _playersInLobby = new();

        [ViewVariables]
        private TimeSpan _roundStartTime;

        [ViewVariables]
        private TimeSpan _pauseTime;

        [ViewVariables]
        public bool Paused { get; set; }

        [ViewVariables]
        private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;

        private void UpdateInfoText()
        {
            var infoMsg = GetInfoMsg();

            _netManager.ServerSendToMany(infoMsg, _playersInLobby.Keys.Select(p => p.ConnectedClient).ToList());
        }

        private string GetInfoText()
        {
            if (Preset == null)
            {
                return string.Empty;
            }

            var gmTitle = Preset.ModeTitle;
            var desc = Preset.Description;
            return Loc.GetString(@"Hi and welcome to [color=white]Space Station 14![/color]

The current game mode is: [color=white]{0}[/color].
[color=yellow]{1}[/color]", gmTitle, desc);
        }

        private MsgTickerLobbyReady GetStatusSingle(IPlayerSession player, PlayerStatus status)
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetUserId, PlayerStatus>
            {
                { player.UserId, status }
            };
            return msg;
        }

        private MsgTickerLobbyReady GetPlayerStatus()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetUserId, PlayerStatus>();
            foreach (var player in _playersInLobby.Keys)
            {
                _playersInLobby.TryGetValue(player, out var status);
                msg.PlayerStatus.Add(player.UserId, status);
            }
            return msg;
        }

        private MsgTickerLobbyStatus _getStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var status);
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyStatus>();
            msg.IsRoundStarted = RunLevel != GameRunLevel.PreRoundLobby;
            msg.StartTime = _roundStartTime;
            msg.YouAreReady = status == PlayerStatus.Ready;
            msg.Paused = Paused;
            msg.LobbySong = LobbySong;
            return msg;
        }

        private void _sendStatusToAll()
        {
            foreach (var player in _playersInLobby.Keys)
            {
                _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            }
        }

        private MsgTickerLobbyInfo GetInfoMsg()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyInfo>();
            msg.TextBlob = GetInfoText();
            return msg;
        }

        private void UpdateLateJoinStatus()
        {
            var msg = new MsgTickerLateJoinStatus(null!) {Disallowed = DisallowLateJoin};
            _netManager.ServerSendToAll(msg);
        }

        public bool PauseStart(bool pause = true)
        {
            if (Paused == pause)
            {
                return false;
            }

            Paused = pause;

            if (pause)
            {
                _pauseTime = _gameTiming.CurTime;
            }
            else if (_pauseTime != default)
            {
                _roundStartTime += _gameTiming.CurTime - _pauseTime;
            }

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTime;
            lobbyCountdownMessage.Paused = Paused;
            _netManager.ServerSendToAll(lobbyCountdownMessage);

            _chatManager.DispatchServerAnnouncement(Paused
                ? "Round start has been paused."
                : "Round start countdown is now resumed.");

            return true;
        }

        public bool TogglePause()
        {
            PauseStart(!Paused);
            return Paused;
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            if (!_prefsManager.HavePreferencesLoaded(player))
            {
                return;
            }

            var status = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _playersInLobby[player] = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            _netManager.ServerSendToAll(GetStatusSingle(player, status));
        }
    }
}
