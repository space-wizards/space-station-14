using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Spawners.Components;
using Content.Server.Speech.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly AdminSystem _admin = default!;

        public static readonly EntProtoId ObserverPrototypeName = "MobObserver";
        public static readonly EntProtoId AdminObserverPrototypeName = "AdminObserver";

        /// <summary>
        /// How many players have joined the round through normal methods.
        /// Useful for game rules to look at. Doesn't count observers, people in lobby, etc.
        /// </summary>
        public int PlayersJoinedRoundNormally;

        // Mainly to avoid allocations.
        private readonly List<EntityCoordinates> _possiblePositions = new();

        private List<EntityUid> GetSpawnableStations()
        {
            var spawnableStations = new List<EntityUid>();
            var query = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
            while (query.MoveNext(out var uid, out _, out _))
            {
                spawnableStations.Add(uid);
            }

            return spawnableStations;
        }

        private void SpawnPlayers(List<ICommonSession> readyPlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
            bool force)
        {
            // Allow game rules to spawn players by themselves if needed. (For example, nuke ops or wizard)
            RaiseLocalEvent(new RulePlayerSpawningEvent(readyPlayers, profiles, force));

            var playerNetIds = readyPlayers.Select(o => o.UserId).ToHashSet();

            // RulePlayerSpawning feeds a readonlydictionary of profiles.
            // We need to take these players out of the pool of players available as they've been used.
            if (readyPlayers.Count != profiles.Count)
            {
                var toRemove = new RemQueue<NetUserId>();

                foreach (var (player, _) in profiles)
                {
                    if (playerNetIds.Contains(player))
                        continue;

                    toRemove.Add(player);
                }

                foreach (var player in toRemove)
                {
                    profiles.Remove(player);
                }
            }

            var spawnableStations = GetSpawnableStations();
            var assignedJobs = _stationJobs.AssignJobs(profiles, spawnableStations);

            _stationJobs.AssignOverflowJobs(ref assignedJobs, playerNetIds, profiles, spawnableStations);

            // Calculate extended access for stations.
            var stationJobCounts = spawnableStations.ToDictionary(e => e, _ => 0);
            foreach (var (netUser, (job, station)) in assignedJobs)
            {
                if (job == null)
                {
                    var playerSession = _playerManager.GetSessionById(netUser);
                    var evNoJobs = new NoJobsAvailableSpawningEvent(playerSession); // Used by gamerules to wipe their antag slot, if they got one
                    RaiseLocalEvent(evNoJobs);

                    _chatManager.DispatchServerMessage(playerSession, Loc.GetString("job-not-available-wait-in-lobby"));
                }
                else
                {
                    stationJobCounts[station] += 1;
                }
            }

            _stationJobs.CalcExtendedAccess(stationJobCounts);

            // Spawn everybody in!
            foreach (var (player, (job, station)) in assignedJobs)
            {
                if (job == null)
                    continue;

                SpawnPlayer(_playerManager.GetSessionById(player), profiles[player], station, job, false);
            }

            RefreshLateJoinAllowed();

            // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
            RaiseLocalEvent(new RulePlayerJobsAssignedEvent(
                assignedJobs.Keys.Select(x => _playerManager.GetSessionById(x)).ToArray(),
                profiles,
                force));
        }

        private void SpawnPlayer(ICommonSession player,
            EntityUid station,
            string? jobId = null,
            bool lateJoin = true,
            bool silent = false)
        {
            var character = GetPlayerProfile(player);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans == null || jobId != null && jobBans.Contains(jobId)) //TODO: use IsRoleBanned directly?
                return;

            if (jobId != null)
            {
                var jobs = new List<ProtoId<JobPrototype>> {jobId};
                var ev = new IsRoleAllowedEvent(player, jobs, null);
                RaiseLocalEvent(ref ev);
                if (ev.Cancelled)
                    return;
            }

            SpawnPlayer(player, character, station, jobId, lateJoin, silent);
        }

        private void SpawnPlayer(ICommonSession player,
            HumanoidCharacterProfile character,
            EntityUid station,
            string? jobId = null,
            bool lateJoin = true,
            bool silent = false)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            if (station == EntityUid.Invalid)
            {
                var stations = GetSpawnableStations();
                _robustRandom.Shuffle(stations);
                if (stations.Count == 0)
                    station = EntityUid.Invalid;
                else
                    station = stations[0];
            }

            if (lateJoin && DisallowLateJoin)
            {
                JoinAsObserver(player);
                return;
            }

            string speciesId;
            if (_randomizeCharacters)
            {
                var weightId = _cfg.GetCVar(CCVars.ICRandomSpeciesWeights);

                // If blank, choose a round start species.
                if (string.IsNullOrEmpty(weightId))
                {
                    var roundStart = new List<ProtoId<SpeciesPrototype>>();

                    var speciesPrototypes = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>();
                    foreach (var proto in speciesPrototypes)
                    {
                        if (proto.RoundStart)
                            roundStart.Add(proto.ID);
                    }

                    speciesId = roundStart.Count == 0
                        ? SharedHumanoidAppearanceSystem.DefaultSpecies
                        : _robustRandom.Pick(roundStart);
                }
                else
                {
                    var weights = _prototypeManager.Index<WeightedRandomSpeciesPrototype>(weightId);
                    speciesId = weights.Pick(_robustRandom);
                }

                character = HumanoidCharacterProfile.RandomWithSpecies(speciesId);
            }

            // We raise this event to allow other systems to handle spawning this player themselves. (e.g. late-join wizard, etc)
            var bev = new PlayerBeforeSpawnEvent(player, character, jobId, lateJoin, station);
            RaiseLocalEvent(bev);

            // Do nothing, something else has handled spawning this player for us!
            if (bev.Handled)
            {
                PlayerJoinGame(player, silent);
                return;
            }

            // Figure out job restrictions
            var restrictedRoles = new HashSet<ProtoId<JobPrototype>>();
            var ev = new GetDisallowedJobsEvent(player, restrictedRoles);
            RaiseLocalEvent(ref ev);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans != null)
                restrictedRoles.UnionWith(jobBans);

            // Pick best job best on prefs.
            jobId ??= _stationJobs.PickBestAvailableJobWithPriority(station,
                character.JobPriorities,
                true,
                restrictedRoles);
            // If no job available, stay in lobby, or if no lobby spawn as observer
            if (jobId is null)
            {
                if (!LobbyEnabled)
                {
                    JoinAsObserver(player);
                }

                var evNoJobs = new NoJobsAvailableSpawningEvent(player); // Used by gamerules to wipe their antag slot, if they got one
                RaiseLocalEvent(evNoJobs);

                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("game-ticker-player-no-jobs-available-when-joining"));
                return;
            }

            DoSpawn(player, character, station, jobId, silent, out var mob, out var jobPrototype, out var jobName);

            if (lateJoin && !silent)
            {
                if (jobPrototype.JoinNotifyCrew)
                {
                    _chatSystem.DispatchStationAnnouncement(station,
                        Loc.GetString("latejoin-arrival-announcement-special",
                            ("character", MetaData(mob).EntityName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        Loc.GetString("latejoin-arrival-sender"),
                        playDefaultSound: false,
                        colorOverride: Color.Gold);
                }
                else
                {
                    _chatSystem.DispatchStationAnnouncement(station,
                        Loc.GetString("latejoin-arrival-announcement",
                            ("character", MetaData(mob).EntityName),
                            ("entity", mob),
                            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
                        Loc.GetString("latejoin-arrival-sender"),
                        playDefaultSound: false);
                }
            }

            if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                AddComp<OwOAccentComponent>(mob);
            }

            _stationJobs.TryAssignJob(station, jobPrototype, player.UserId);

            if (lateJoin)
            {
                _adminLogger.Add(LogType.LateJoin,
                    LogImpact.Medium,
                    $"Player {player.Name} late joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
            }
            else
            {
                _adminLogger.Add(LogType.RoundStartJoin,
                    LogImpact.Medium,
                    $"Player {player.Name} joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
            }

            // Make sure they're aware of extended access.
            if (Comp<StationJobsComponent>(station).ExtendedAccess
                && (jobPrototype.ExtendedAccess.Count > 0 || jobPrototype.ExtendedAccessGroups.Count > 0))
            {
                _chatManager.DispatchServerMessage(player, Loc.GetString("job-greet-crew-shortages"));
            }

            if (!silent && TryComp(station, out MetaDataComponent? metaData))
            {
                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("job-greet-station-name", ("stationName", metaData.EntityName)));
            }

            // We raise this event directed to the mob, but also broadcast it so game rules can do something now.
            PlayersJoinedRoundNormally++;
            var aev = new PlayerSpawnCompleteEvent(mob,
                player,
                jobId,
                lateJoin,
                silent,
                PlayersJoinedRoundNormally,
                station,
                character);
            RaiseLocalEvent(mob, aev, true);
        }

        /// <summary>
        /// Creates a mob on the specified station, creates the new mind, equips job-specific starting gear and loadout
        /// </summary>
        public void DoSpawn(
            ICommonSession player,
            HumanoidCharacterProfile character,
            EntityUid station,
            string jobId,
            bool silent,
            out EntityUid mob,
            out JobPrototype jobPrototype,
            out string jobName)
        {
            PlayerJoinGame(player, silent);

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            var newMind = _mind.CreateMind(data!.UserId, character.Name);
            _mind.SetUserId(newMind, data.UserId);

            jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);

            _playTimeTrackings.PlayerRolesChanged(player);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, jobId, character);
            DebugTools.AssertNotNull(mobMaybe);
            mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);

            _roles.MindAddJobRole(newMind, silent: silent, jobPrototype: jobId);
            jobName = _jobs.MindTryGetJobName(newMind);
            _admin.UpdatePlayerList(player);
        }

        public void Respawn(ICommonSession player)
        {
            _mind.WipeMind(player);
            _adminLogger.Add(LogType.Respawn, LogImpact.Medium, $"Player {player} was respawned.");

            if (LobbyEnabled)
                PlayerJoinLobby(player);
            else
                SpawnPlayer(player, EntityUid.Invalid);
        }

        /// <summary>
        /// Makes a player join into the game and spawn on a station.
        /// </summary>
        /// <param name="player">The player joining</param>
        /// <param name="station">The station they're spawning on</param>
        /// <param name="jobId">An optional job for them to spawn as</param>
        /// <param name="silent">Whether or not the player should be greeted upon joining</param>
        public void MakeJoinGame(ICommonSession player, EntityUid station, string? jobId = null, bool silent = false)
        {
            if (!_playerGameStatuses.ContainsKey(player.UserId))
                return;

            if (!_userDb.IsLoadComplete(player))
                return;

            SpawnPlayer(player, station, jobId, silent: silent);
        }

        /// <summary>
        /// Causes the given player to join the current game as observer ghost. See also <see cref="SpawnObserver"/>
        /// </summary>
        public void JoinAsObserver(ICommonSession player)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            PlayerJoinGame(player);
            SpawnObserver(player);
        }

        /// <summary>
        /// Spawns an observer ghost and attaches the given player to it. If the player does not yet have a mind, the
        /// player is given a new mind with the observer role. Otherwise, the current mind is transferred to the ghost.
        /// </summary>
        public void SpawnObserver(ICommonSession player)
        {
            if (DummyTicker)
                return;

            var makeObserver = false;
            Entity<MindComponent?>? mind = player.GetMind();
            if (mind == null)
            {
                var name = GetPlayerProfile(player).Name;
                var (mindId, mindComp) = _mind.CreateMind(player.UserId, name);
                mind = (mindId, mindComp);
                _mind.SetUserId(mind.Value, player.UserId);
                makeObserver = true;
            }

            var ghost = _ghost.SpawnGhost(mind.Value);
            if (makeObserver)
                _roles.MindAddRole(mind.Value, "MindRoleObserver");

            _adminLogger.Add(LogType.LateJoin,
                LogImpact.Low,
                $"{player.Name} late joined the round as an Observer with {ToPrettyString(ghost):entity}.");
        }

        #region Spawn Points

        public EntityCoordinates GetObserverSpawnPoint()
        {
            _possiblePositions.Clear();
            var spawnPointQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (spawnPointQuery.MoveNext(out var uid, out var point, out var transform))
            {
                if (point.SpawnType != SpawnPointType.Observer
                   || TerminatingOrDeleted(uid)
                   || transform.MapUid == null
                   || TerminatingOrDeleted(transform.MapUid.Value))
                {
                    continue;
                }

                _possiblePositions.Add(transform.Coordinates);
            }

            var metaQuery = GetEntityQuery<MetaDataComponent>();

            // Fallback to a random grid.
            if (_possiblePositions.Count == 0)
            {
                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!metaQuery.TryGetComponent(uid, out var meta) || meta.EntityPaused || TerminatingOrDeleted(uid))
                    {
                        continue;
                    }

                    _possiblePositions.Add(new EntityCoordinates(uid, Vector2.Zero));
                }
            }

            if (_possiblePositions.Count != 0)
            {
                // TODO: This is just here for the eye lerping.
                // Ideally engine would just spawn them on grid directly I guess? Right now grid traversal is handling it during
                // update which means we need to add a hack somewhere around it.
                var spawn = _robustRandom.Pick(_possiblePositions);
                var toMap = _transform.ToMapCoordinates(spawn);

                if (_mapManager.TryFindGridAt(toMap, out var gridUid, out _))
                {
                    var gridXform = Transform(gridUid);

                    return new EntityCoordinates(gridUid, Vector2.Transform(toMap.Position, _transform.GetInvWorldMatrix(gridXform)));
                }

                return spawn;
            }

            if (_map.MapExists(DefaultMap))
            {
                var mapUid = _map.GetMapOrInvalid(DefaultMap);
                if (!TerminatingOrDeleted(mapUid))
                    return new EntityCoordinates(mapUid, Vector2.Zero);
            }

            // Just pick a point at this point I guess.
            foreach (var map in _map.GetAllMapIds())
            {
                var mapUid = _map.GetMapOrInvalid(map);

                if (!metaQuery.TryGetComponent(mapUid, out var meta)
                    || meta.EntityPaused
                    || TerminatingOrDeleted(mapUid))
                {
                    continue;
                }

                return new EntityCoordinates(mapUid, Vector2.Zero);
            }

            // AAAAAAAAAAAAA
            // This should be an error, if it didn't cause tests to start erroring when they delete a player.
            _sawmill.Warning("Found no observer spawn points!");
            return EntityCoordinates.Invalid;
        }

        #endregion
    }
}
