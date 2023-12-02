using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Server.Ghost;
using Content.Server.Spawners.Components;
using Content.Server.Speech.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
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

        [ValidatePrototypeId<EntityPrototype>]
        public const string ObserverPrototypeName = "MobObserver";

        [ValidatePrototypeId<EntityPrototype>]
        public const string AdminObserverPrototypeName = "AdminObserver";

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

        private void SpawnPlayers(List<ICommonSession> readyPlayers, Dictionary<NetUserId, HumanoidCharacterProfile> profiles, bool force)
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
                    var playerSession = _playerManager.GetSessionByUserId(netUser);
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

                SpawnPlayer(_playerManager.GetSessionByUserId(player), profiles[player], station, job, false);
            }

            RefreshLateJoinAllowed();

            // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
            RaiseLocalEvent(new RulePlayerJobsAssignedEvent(assignedJobs.Keys.Select(x => _playerManager.GetSessionByUserId(x)).ToArray(), profiles, force));
        }

        private void SpawnPlayer(ICommonSession player, EntityUid station, string? jobId = null, bool lateJoin = true, bool silent = false)
        {
            var character = GetPlayerProfile(player);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans == null || jobId != null && jobBans.Contains(jobId))
                return;

            if (jobId != null && !_playTimeTrackings.IsAllowed(player, jobId))
                return;
            SpawnPlayer(player, character, station, jobId, lateJoin, silent);
        }

        private void SpawnPlayer(ICommonSession player, HumanoidCharacterProfile character, EntityUid station, string? jobId = null, bool lateJoin = true, bool silent = false)
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

            // Automatically de-admin players who are joining.
            if (_cfg.GetCVar(CCVars.AdminDeadminOnJoin) && _adminManager.IsAdmin(player))
                _adminManager.DeAdmin(player);

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
            var restrictedRoles = new HashSet<string>();

            var getDisallowed = _playTimeTrackings.GetDisallowedJobs(player);
            restrictedRoles.UnionWith(getDisallowed);

            var jobBans = _banManager.GetJobBans(player.UserId);
            if (jobBans != null)
                restrictedRoles.UnionWith(jobBans);

            // Pick best job best on prefs.
            jobId ??= _stationJobs.PickBestAvailableJobWithPriority(station, character.JobPriorities, true,
                restrictedRoles);
            // If no job available, stay in lobby, or if no lobby spawn as observer
            if (jobId is null)
            {
                if (!LobbyEnabled)
                {
                    JoinAsObserver(player);
                }
                _chatManager.DispatchServerMessage(player, Loc.GetString("game-ticker-player-no-jobs-available-when-joining"));
                return;
            }

            PlayerJoinGame(player, silent);

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            var newMind = _mind.CreateMind(data!.UserId, character.Name);
            _mind.SetUserId(newMind, data.UserId);

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
            var job = new JobComponent { Prototype = jobId };
            _roles.MindAddRole(newMind, job, silent: silent);
            var jobName = _jobs.MindTryGetJobName(newMind);

            _playTimeTrackings.PlayerRolesChanged(player);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, job, character);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);

            if (lateJoin && !silent)
            {
                _chatSystem.DispatchStationAnnouncement(station,
                    Loc.GetString(
                        "latejoin-arrival-announcement",
                    ("character", MetaData(mob).EntityName),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))
                    ), Loc.GetString("latejoin-arrival-sender"),
                    playDefaultSound: false);
            }

            if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                EntityManager.AddComponent<OwOAccentComponent>(mob);
            }

            _stationJobs.TryAssignJob(station, jobPrototype);

            if (lateJoin)
                _adminLogger.Add(LogType.LateJoin, LogImpact.Medium, $"Player {player.Name} late joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
            else
                _adminLogger.Add(LogType.RoundStartJoin, LogImpact.Medium, $"Player {player.Name} joined as {character.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");

            // Make sure they're aware of extended access.
            if (Comp<StationJobsComponent>(station).ExtendedAccess
                && (jobPrototype.ExtendedAccess.Count > 0
                    || jobPrototype.ExtendedAccessGroups.Count > 0))
            {
                _chatManager.DispatchServerMessage(player, Loc.GetString("job-greet-crew-shortages"));
            }

            if (!silent && TryComp(station, out MetaDataComponent? metaData))
            {
                _chatManager.DispatchServerMessage(player,
                    Loc.GetString("job-greet-station-name", ("stationName", metaData.EntityName)));
            }

            // Arrivals is unable to do this during spawning as no actor is attached yet.
            // We also want this message last.
            if (!silent && lateJoin && _arrivals.Enabled)
            {
                var arrival = _arrivals.NextShuttleArrival();
                if (arrival == null)
                {
                    _chatManager.DispatchServerMessage(player, Loc.GetString("latejoin-arrivals-direction"));
                }
                else
                {
                    _chatManager.DispatchServerMessage(player, Loc.GetString("latejoin-arrivals-direction-time",
                        ("time", $"{arrival:mm\\:ss}")));
                }
            }

            // We raise this event directed to the mob, but also broadcast it so game rules can do something now.
            PlayersJoinedRoundNormally++;
            var aev = new PlayerSpawnCompleteEvent(mob, player, jobId, lateJoin, PlayersJoinedRoundNormally, station, character);
            RaiseLocalEvent(mob, aev, true);
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
        /// Makes a player join into the game and spawn on a staiton.
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

            var mind = player.GetMind();
            if (mind == null)
            {
                mind = _mind.CreateMind(player.UserId);
                _mind.SetUserId(mind.Value, player.UserId);
                _roles.MindAddRole(mind.Value, new ObserverRoleComponent());
            }

            var name = GetPlayerProfile(player).Name;
            var ghost = SpawnObserverMob();
            _metaData.SetEntityName(ghost, name);
            _ghost.SetCanReturnToBody(ghost, false);
            _mind.TransferTo(mind.Value, ghost);
        }

        #region Mob Spawning Helpers
        private EntityUid SpawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return EntityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }
        #endregion

        #region Spawn Points
        public EntityCoordinates GetObserverSpawnPoint()
        {
            _possiblePositions.Clear();

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
            {
                if (point.SpawnType != SpawnPointType.Observer)
                    continue;

                _possiblePositions.Add(transform.Coordinates);
            }

            var metaQuery = GetEntityQuery<MetaDataComponent>();

            // Fallback to a random grid.
            if (_possiblePositions.Count == 0)
            {
                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!metaQuery.TryGetComponent(uid, out var meta) ||
                        meta.EntityPaused)
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
                var toMap = spawn.ToMap(EntityManager);

                if (_mapManager.TryFindGridAt(toMap, out var gridUid, out _))
                {
                    var gridXform = Transform(gridUid);

                    return new EntityCoordinates(gridUid,
                        gridXform.InvWorldMatrix.Transform(toMap.Position));
                }

                return spawn;
            }

            if (_mapManager.MapExists(DefaultMap))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(DefaultMap), Vector2.Zero);
            }

            // Just pick a point at this point I guess.
            foreach (var map in _mapManager.GetAllMapIds())
            {
                var mapUid = _mapManager.GetMapEntityId(map);

                if (!metaQuery.TryGetComponent(mapUid, out var meta) ||
                    meta.EntityPaused)
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

    /// <summary>
    ///     Event raised broadcast before a player is spawned by the GameTicker.
    ///     You can use this event to spawn a player off-station on late-join but also at round start.
    ///     When this event is handled, the GameTicker will not perform its own player-spawning logic.
    /// </summary>
    [PublicAPI]
    public sealed class PlayerBeforeSpawnEvent : HandledEntityEventArgs
    {
        public ICommonSession Player { get; }
        public HumanoidCharacterProfile Profile { get; }
        public string? JobId { get; }
        public bool LateJoin { get; }
        public EntityUid Station { get; }

        public PlayerBeforeSpawnEvent(ICommonSession player, HumanoidCharacterProfile profile, string? jobId, bool lateJoin, EntityUid station)
        {
            Player = player;
            Profile = profile;
            JobId = jobId;
            LateJoin = lateJoin;
            Station = station;
        }
    }

    /// <summary>
    ///     Event raised both directed and broadcast when a player has been spawned by the GameTicker.
    ///     You can use this to handle people late-joining, or to handle people being spawned at round start.
    ///     Can be used to give random players a role, modify their equipment, etc.
    /// </summary>
    [PublicAPI]
    public sealed class PlayerSpawnCompleteEvent : EntityEventArgs
    {
        public EntityUid Mob { get; }
        public ICommonSession Player { get; }
        public string? JobId { get; }
        public bool LateJoin { get; }
        public EntityUid Station { get; }
        public HumanoidCharacterProfile Profile { get; }

        // Ex. If this is the 27th person to join, this will be 27.
        public int JoinOrder { get; }

        public PlayerSpawnCompleteEvent(EntityUid mob, ICommonSession player, string? jobId, bool lateJoin, int joinOrder, EntityUid station, HumanoidCharacterProfile profile)
        {
            Mob = mob;
            Player = player;
            JobId = jobId;
            LateJoin = lateJoin;
            Station = station;
            Profile = profile;
            JoinOrder = joinOrder;
        }
    }
}
