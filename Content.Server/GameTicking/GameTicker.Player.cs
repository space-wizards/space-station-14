using Content.Server.Database;
using Content.Server.Players;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using PlayerData = Content.Server.Players.PlayerData;

namespace Content.Server.GameTicking
{
    [UsedImplicitly]
    public sealed partial class GameTicker
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;

        private void InitializePlayer()
        {
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private async void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            if (_mindSystem.TryGetMind(session.UserId, out var mind))
            {
                if (args.OldStatus == SessionStatus.Connecting && args.NewStatus == SessionStatus.Connected)
                    mind.Session = session;

                DebugTools.Assert(mind.Session == session);
                DebugTools.Assert(session.Data.ContentData()?.Mind is not {} dataMind || dataMind == mind);
            }

            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    AddPlayerToDb(args.Session.UserId.UserId);

                    // Always make sure the client has player data.
                    if (session.Data.ContentDataUncast == null)
                    {
                        var data = new PlayerData(session.UserId, args.Session.Name);
                        data.Mind = mind;
                        session.Data.ContentDataUncast = data;
                    }

                    // Make the player actually join the game.
                    // timer time must be > tick length
                    Timer.Spawn(0, args.Session.JoinGame);

                    var record = await _dbManager.GetPlayerRecordByUserId(args.Session.UserId);
                    var firstConnection = record != null &&
                                          Math.Abs((record.FirstSeenTime - record.LastSeenTime).TotalMinutes) < 1;

                    _chatManager.SendAdminAnnouncement(firstConnection
                        ? Loc.GetString("player-first-join-message", ("name", args.Session.Name))
                        : Loc.GetString("player-join-message", ("name", args.Session.Name)));

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _userDb.ClientConnected(session);

                    var data = session.ContentData();

                    DebugTools.AssertNotNull(data);

                    if (data!.Mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            PlayerJoinLobby(session);
                            return;
                        }

                        SpawnWaitDb();
                        break;
                    }

                    if (data.Mind.CurrentEntity == null || Deleted(data.Mind.CurrentEntity))
                    {
                        DebugTools.Assert(data.Mind.CurrentEntity == null, "a mind's current entity has been deleted");
                        SpawnWaitDb();
                    }
                    else
                    {
                        session.AttachToEntity(data.Mind.CurrentEntity);
                        PlayerJoinGame(session);
                    }
                    break;
                }

                case SessionStatus.Disconnected:
                {
                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));
                    if (mind != null)
                        mind.Session = null;

                    _userDb.ClientDisconnected(session);
                    break;
                }
            }
            //When the status of a player changes, update the server info text
            UpdateInfoText();

            async void SpawnWaitDb()
            {
                await _userDb.WaitLoadComplete(session);
                SpawnPlayer(session, EntityUid.Invalid);
            }

            async void AddPlayerToDb(Guid id)
            {
                if (RoundId != 0 && _runLevel != GameRunLevel.PreRoundLobby)
                {
                    await _db.AddRoundPlayers(RoundId, id);
                }
            }
        }

        private HumanoidCharacterProfile GetPlayerProfile(IPlayerSession p)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        public void PlayerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session, Loc.GetString("game-ticker-player-join-game-message"));

            _playerGameStatuses[session.UserId] = PlayerGameStatus.JoinedGame;
            _db.AddRoundPlayers(RoundId, session.UserId);

            RaiseNetworkEvent(new TickerJoinGameEvent(), session.ConnectedClient);
        }

        private void PlayerJoinLobby(IPlayerSession session)
        {
            _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            _db.AddRoundPlayers(RoundId, session.UserId);

            var client = session.ConnectedClient;
            RaiseNetworkEvent(new TickerJoinLobbyEvent(), client);
            RaiseNetworkEvent(GetStatusMsg(session), client);
            RaiseNetworkEvent(GetInfoMsg(), client);
            RaiseNetworkEvent(GetPlayerStatus(), client);
            RaiseLocalEvent(new PlayerJoinedLobbyEvent(session));
        }

        private void ReqWindowAttentionAll()
        {
            RaiseNetworkEvent(new RequestWindowAttentionEvent());
        }
    }

    public sealed class PlayerJoinedLobbyEvent : EntityEventArgs
    {
        public readonly IPlayerSession PlayerSession;

        public PlayerJoinedLobbyEvent(IPlayerSession playerSession)
        {
            PlayerSession = playerSession;
        }
    }
}
