using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Players;
using Content.Server.Mind;
using Content.Server.Ghost;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

        [ViewVariables]
        private TimeSpan _roundStartTimeSpan;

        [ViewVariables]
        private GameRunLevel _runLevel;

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                RaiseLocalEvent(new GameRunLevelChangedEvent(old, value));
            }
        }

        private void PreRoundSetup()
        {
            DefaultMap = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var map = ChosenMap;
            var grid = _mapLoader.LoadBlueprint(DefaultMap, map);

            if (grid == null)
            {
                throw new InvalidOperationException($"No grid found for map {map}");
            }

            var stationXform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);

            if (StationOffset)
            {
                // Apply a random offset to the station grid entity.
                var x = _robustRandom.NextFloat() * MaxStationOffset * 2 - MaxStationOffset;
                var y = _robustRandom.NextFloat() * MaxStationOffset * 2 - MaxStationOffset;
                stationXform.LocalPosition = new Vector2(x, y);
            }

            if (StationRotation)
            {
                stationXform.LocalRotation = _robustRandom.NextFloat(MathF.Tau);
            }

            DefaultGridId = grid.Index;
            _spawnPoint = grid.ToCoordinates();

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        public void StartRound(bool force = false)
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            SendServerMessage(Loc.GetString("game-ticker-start-round"));

            List<IPlayerSession> readyPlayers;
            if (LobbyEnabled)
            {
                readyPlayers = _playersInLobby.Where(p => p.Value == LobbyPlayerStatus.Ready).Select(p => p.Key).ToList();
            }
            else
            {
                readyPlayers = _playersInLobby.Keys.ToList();
            }

            _roundStartDateTime = DateTime.UtcNow;
            RunLevel = GameRunLevel.InRound;

            RoundLengthMetric.Set(0);

            // Get the profiles for each player for easier lookup.
            var profiles = _prefsManager.GetSelectedProfilesForPlayers(
                readyPlayers
                    .Select(p => p.UserId).ToList())
                    .ToDictionary(p => p.Key, p => (HumanoidCharacterProfile) p.Value);

            foreach (var readyPlayer in readyPlayers)
            {
                if (!profiles.ContainsKey(readyPlayer.UserId))
                {
                    profiles.Add(readyPlayer.UserId, HumanoidCharacterProfile.Random());
                }
            }

            var assignedJobs = AssignJobs(readyPlayers, profiles);

            // For players without jobs, give them the overflow job if they have that set...
            foreach (var player in readyPlayers)
            {
                if (assignedJobs.ContainsKey(player))
                {
                    continue;
                }

                var profile = profiles[player.UserId];
                if (profile.PreferenceUnavailable == PreferenceUnavailableMode.SpawnAsOverflow)
                {
                    assignedJobs.Add(player, OverflowJob);
                }
            }

            // Spawn everybody in!
            foreach (var (player, job) in assignedJobs)
            {
                SpawnPlayer(player, profiles[player.UserId], job, false);
            }

            // Time to start the preset.
            Preset = MakeGamePreset(profiles);

            DisallowLateJoin |= Preset.DisallowLateJoin;

            if (!Preset.Start(assignedJobs.Keys.ToList(), force))
            {
                if (_configurationManager.GetCVar(CCVars.GameLobbyFallbackEnabled))
                {
                    SetStartPreset(_configurationManager.GetCVar(CCVars.GameLobbyFallbackPreset));
                    var newPreset = MakeGamePreset(profiles);
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("game-ticker-start-round-cannot-start-game-mode-fallback",
                                      ("failedGameMode", Preset.ModeTitle),
                                      ("fallbackMode", newPreset.ModeTitle)));
                    if (!newPreset.Start(readyPlayers, force))
                    {
                        throw new ApplicationException("Fallback preset failed to start!");
                    }

                    DisallowLateJoin = false;
                    DisallowLateJoin |= newPreset.DisallowLateJoin;
                    Preset = newPreset;
                }
                else
                {
                    SendServerMessage(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-restart", ("failedGameMode", Preset.ModeTitle)));
                    RestartRound();
                    DelayStart(TimeSpan.FromSeconds(PresetFailedCooldownIncrease));
                    return;
                }
            }
            Preset.OnGameStarted();

            _roundStartTimeSpan = IoCManager.Resolve<IGameTiming>().RealTime;
            SendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
            UpdateJobsAvailable();
        }

        public void EndRound(string text = "")
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var gamemodeTitle = Preset?.ModeTitle ?? string.Empty;
            var roundEndText = text + $"\n{Preset?.GetRoundEndDescription() ?? string.Empty}";

            //Get the timespan of the round.
            var roundDuration = IoCManager.Resolve<IGameTiming>().RealTime.Subtract(_roundStartTimeSpan);

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
            // Grab the great big book of all the Minds, we'll need them for this.
            var allMinds = EntitySystem.Get<MindTrackerSystem>().AllMinds;
            foreach (var mind in allMinds)
            {
                if (mind != null)
                {
                    // Some basics assuming things fail
                    var userId = mind.OriginalOwnerUserId;
                    var playerOOCName = userId.ToString();
                    var connected = false;
                    var observer = mind.AllRoles.Any(role => role is ObserverRole);
                    // Continuing
                    if (_playerManager.TryGetSessionById(userId, out var ply))
                    {
                        connected = true;
                    }
                    PlayerData? contentPlayerData = null;
                    if (_playerManager.TryGetPlayerData(userId, out var playerData))
                    {
                        contentPlayerData = playerData.ContentData();
                    }
                    // Finish
                    var antag = mind.AllRoles.Any(role => role.Antagonist);
                    var playerEndRoundInfo = new RoundEndMessageEvent.RoundEndPlayerInfo()
                    {
                        // Note that contentPlayerData?.Name sticks around after the player is disconnected.
                        // This is as opposed to ply?.Name which doesn't.
                        PlayerOOCName = contentPlayerData?.Name ?? "(IMPOSSIBLE: REGISTERED MIND WITH NO OWNER)",
                        // Character name takes precedence over current entity name
                        PlayerICName = mind.CharacterName ?? mind.CurrentEntity?.Name,
                        Role = antag
                            ? mind.AllRoles.First(role => role.Antagonist).Name
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("game-ticker-unknown-role"),
                        Antag = antag,
                        Observer = observer,
                        Connected = connected
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }
            // This ordering mechanism isn't great (no ordering of minds) but functions
            var listOfPlayerInfoFinal = listOfPlayerInfo.OrderBy(pi => pi.PlayerOOCName).ToArray();

            RaiseNetworkEvent(new RoundEndMessageEvent(gamemodeTitle, roundEndText, roundDuration, listOfPlayerInfoFinal.Length, listOfPlayerInfoFinal));
        }

        public void RestartRound()
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            if (_updateOnRoundEnd)
            {
                _baseServer.Shutdown(Loc.GetString("game-ticker-shutdown-server-update"));
                return;
            }

            Logger.InfoS("ticker", "Restarting round!");

            SendServerMessage(Loc.GetString("game-ticker-restart-round"));

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            LobbySong = _robustRandom.Pick(_lobbyMusicCollection.PickFiles).ToString();
            ResettingCleanup();
            PreRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                Preset = null;

                if (_playerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTime = _gameTiming.CurTime + LobbyDuration;

                SendStatusToAll();

                ReqWindowAttentionAll();
            }
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void ResettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.GetAllPlayers())
            {
                PlayerJoinLobby(player);
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData())
            {
                unCastData.ContentData()?.WipeMind();
            }

            // Delete all entities.
            foreach (var entity in EntityManager.GetEntities().ToList())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();
            }

            _mapManager.Restart();

            // Clear up any game rules.
            foreach (var rule in _gameRules)
            {
                rule.Removed();
            }

            _gameRules.Clear();

            // Round restart cleanup event, so entity systems can reset.
            var ev = new RoundRestartCleanupEvent();
            RaiseLocalEvent(ev);

            // So clients' entity systems can clean up too...
            RaiseNetworkEvent(ev, Filter.Broadcast());

            _spawnedPositions.Clear();
            _manifest.Clear();
            DisallowLateJoin = false;
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start", ("seconds",time.TotalSeconds)));

            return true;
        }

        private void UpdateRoundFlow(float frameTime)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameTime);
            }

            if (RunLevel != GameRunLevel.PreRoundLobby ||
                Paused ||
                _roundStartTime > _gameTiming.CurTime ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public class GameRunLevelChangedEvent
    {
        public GameRunLevel Old { get; }
        public GameRunLevel New { get; }

        public GameRunLevelChangedEvent(GameRunLevel old, GameRunLevel @new)
        {
            Old = old;
            New = @new;
        }
    }
}
