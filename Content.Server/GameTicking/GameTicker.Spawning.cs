using System.Globalization;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Hands.Components;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Speech.Components;
using Content.Server.Station;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Species;
using Content.Shared.Station;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        private const string ObserverPrototypeName = "MobObserver";

        [Dependency] private readonly IdCardSystem _cardSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        /// <summary>
        /// Can't yet be removed because every test ever seems to depend on it. I'll make removing this a different PR.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private EntityCoordinates _spawnPoint;

        // Mainly to avoid allocations.
        private readonly List<EntityCoordinates> _possiblePositions = new();

        private void SpawnPlayers(List<IPlayerSession> readyPlayers, IPlayerSession[] origReadyPlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles, bool force)
        {
            // Allow game rules to spawn players by themselves if needed. (For example, nuke ops or wizard)
            RaiseLocalEvent(new RulePlayerSpawningEvent(readyPlayers, profiles, force));

            var assignedJobs = AssignJobs(readyPlayers, profiles);

            AssignOverflowJobs(assignedJobs, origReadyPlayers, profiles);

            // Spawn everybody in!
            foreach (var (player, (job, station)) in assignedJobs)
            {
                SpawnPlayer(player, profiles[player.UserId], station, job, false);
            }

            RefreshLateJoinAllowed();

            // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
            RaiseLocalEvent(new RulePlayerJobsAssignedEvent(assignedJobs.Keys.ToArray(), profiles, force));
        }

        private void AssignOverflowJobs(IDictionary<IPlayerSession, (string, StationId)> assignedJobs,
            IPlayerSession[] origReadyPlayers, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
        {
            // For players without jobs, give them the overflow job if they have that set...
            foreach (var player in origReadyPlayers)
            {
                if (assignedJobs.ContainsKey(player))
                {
                    continue;
                }

                var profile = profiles[player.UserId];
                if (profile.PreferenceUnavailable != PreferenceUnavailableMode.SpawnAsOverflow)
                    continue;

                // Pick a random station
                var stations = _stationSystem.StationInfo.Keys.ToList();

                if (stations.Count == 0)
                {
                    assignedJobs.Add(player, (FallbackOverflowJob, StationId.Invalid));
                    continue;
                }

                _robustRandom.Shuffle(stations);

                foreach (var station in stations)
                {
                    // Pick a random overflow job from that station
                    var overflows = _stationSystem.StationInfo[station].MapPrototype.OverflowJobs.Clone();
                    _robustRandom.Shuffle(overflows);

                    // Stations with no overflow slots should simply get skipped over.
                    if (overflows.Count == 0)
                        continue;

                    // If the overflow exists, put them in as it.
                    assignedJobs.Add(player, (overflows[0], stations[0]));
                    break;
                }
            }
        }

        private void SpawnPlayer(IPlayerSession player, StationId station, string? jobId = null, bool lateJoin = true)
        {
            var character = GetPlayerProfile(player);

            var jobBans = _roleBanManager.GetJobBans(player.UserId);
            if (jobBans == null || (jobId != null && jobBans.Contains(jobId)))
                return;
            SpawnPlayer(player, character, station, jobId, lateJoin);
            UpdateJobsAvailable();
        }

        private void SpawnPlayer(IPlayerSession player, HumanoidCharacterProfile character, StationId station, string? jobId = null, bool lateJoin = true)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            if (station == StationId.Invalid)
            {
                var stations = _stationSystem.StationInfo.Keys.ToList();
                _robustRandom.Shuffle(stations);
                if (stations.Count == 0)
                    station = StationId.Invalid;
                else
                    station = stations[0];
            }

            if (lateJoin && DisallowLateJoin)
            {
                MakeObserve(player);
                return;
            }

            // We raise this event to allow other systems to handle spawning this player themselves. (e.g. late-join wizard, etc)
            var bev = new PlayerBeforeSpawnEvent(player, character, jobId, lateJoin, station);
            RaiseLocalEvent(bev);

            // Do nothing, something else has handled spawning this player for us!
            if (bev.Handled)
            {
                PlayerJoinGame(player);
                return;
            }

            // Pick best job best on prefs.
            jobId ??= PickBestAvailableJob(player, character, station);
            // If no job available, stay in lobby, or if no lobby spawn as observer
            if (jobId is null)
            {
                if (!LobbyEnabled)
                {
                    MakeObserve(player);
                }
                _chatManager.DispatchServerMessage(player, Loc.GetString("game-ticker-player-no-jobs-available-when-joining"));
                return;
            }

            PlayerJoinGame(player);

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            data!.WipeMind();
            var newMind = new Mind.Mind(data.UserId)
            {
                CharacterName = character.Name
            };
            newMind.ChangeOwningPlayer(data.UserId);

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
            var job = new Job(newMind, jobPrototype);
            newMind.AddRole(job);

            if (lateJoin)
            {
                _chatManager.DispatchStationAnnouncement(Loc.GetString(
                    "latejoin-arrival-announcement",
                    ("character", character.Name),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.Name))
                    ), Loc.GetString("latejoin-arrival-sender"),
                    playDefaultSound: false);
            }

            var mob = SpawnPlayerMob(job, character, station, lateJoin);
            newMind.TransferTo(mob);

            if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                EntityManager.AddComponent<OwOAccentComponent>(mob);
            }

            AddManifestEntry(character.Name, jobId);
            AddSpawnedPosition(jobId);
            EquipIdCard(mob, character.Name, jobPrototype);

            foreach (var jobSpecial in jobPrototype.Special)
            {
                jobSpecial.AfterEquip(mob);
            }

            _stationSystem.TryAssignJobToStation(station, jobPrototype);

            if (lateJoin)
                _adminLogSystem.Add(LogType.LateJoin, LogImpact.Medium, $"Player {player.Name} late joined as {character.Name:characterName} on station {_stationSystem.StationInfo[station].Name:stationName} with {ToPrettyString(mob):entity} as a {job.Name:jobName}.");
            else
                _adminLogSystem.Add(LogType.RoundStartJoin, LogImpact.Medium, $"Player {player.Name} joined as {character.Name:characterName} on station {_stationSystem.StationInfo[station].Name:stationName} with {ToPrettyString(mob):entity} as a {job.Name:jobName}.");

            // We raise this event directed to the mob, but also broadcast it so game rules can do something now.
            var aev = new PlayerSpawnCompleteEvent(mob, player, jobId, lateJoin, station, character);
            RaiseLocalEvent(mob, aev);
        }

        public void Respawn(IPlayerSession player)
        {
            player.ContentData()?.WipeMind();
            _adminLogSystem.Add(LogType.Respawn, LogImpact.Medium, $"Player {player} was respawned.");

            if (LobbyEnabled)
                PlayerJoinLobby(player);
            else
                SpawnPlayer(player, StationId.Invalid);
        }

        public void MakeJoinGame(IPlayerSession player, StationId station, string? jobId = null)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            if (!_prefsManager.HavePreferencesLoaded(player))
            {
                return;
            }

            SpawnPlayer(player, station, jobId);
        }

        public void MakeObserve(IPlayerSession player)
        {
            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            PlayerJoinGame(player);

            var name = GetPlayerProfile(player).Name;

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            data!.WipeMind();
            var newMind = new Mind.Mind(data.UserId);
            newMind.ChangeOwningPlayer(data.UserId);
            newMind.AddRole(new ObserverRole(newMind));

            var mob = SpawnObserverMob();
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;
            var ghost = EntityManager.GetComponent<GhostComponent>(mob);
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(ghost, false);
            newMind.TransferTo(mob);

            _playersInLobby[player] = LobbyPlayerStatus.Observer;
            RaiseNetworkEvent(GetStatusSingle(player, LobbyPlayerStatus.Observer));
        }

        #region Mob Spawning Helpers
        private EntityUid SpawnPlayerMob(Job job, HumanoidCharacterProfile? profile, StationId station, bool lateJoin = true)
        {
            var coordinates = lateJoin ? GetLateJoinSpawnPoint(station) : GetJobSpawnPoint(job.Prototype.ID, station);
            var entity = EntityManager.SpawnEntity(
                _prototypeManager.Index<SpeciesPrototype>(profile?.Species ?? SpeciesManager.DefaultSpecies).Prototype,
                coordinates);

            if (job.StartingGear != null)
            {
                var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
                EquipStartingGear(entity, startingGear, profile);
            }

            if (profile != null)
            {
                _humanoidAppearanceSystem.UpdateFromProfile(entity, profile);
                EntityManager.GetComponent<MetaDataComponent>(entity).EntityName = profile.Name;
            }

            return entity;
        }

        private EntityUid SpawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return EntityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }
        #endregion

        #region Equip Helpers
        public void EquipStartingGear(EntityUid entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
        {
            if (_inventorySystem.TryGetSlots(entity, out var slotDefinitions))
            {
                foreach (var slot in slotDefinitions)
                {
                    var equipmentStr = startingGear.GetGear(slot.Name, profile);
                    if (!string.IsNullOrEmpty(equipmentStr))
                    {
                        var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                        _inventorySystem.TryEquip(entity, equipmentEntity, slot.Name, true);
                    }
                }
            }

            if (EntityManager.TryGetComponent(entity, out HandsComponent? handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var (hand, prototype) in inhand)
                {
                    var inhandEntity = EntityManager.SpawnEntity(prototype, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    handsComponent.TryPickupEntity(hand, inhandEntity, checkActionBlocker: false);
                }
            }
        }

        public void EquipIdCard(EntityUid entity, string characterName, JobPrototype jobPrototype)
        {
            if (!_inventorySystem.TryGetSlotEntity(entity, "id", out var idUid))
                return;

            if (!EntityManager.TryGetComponent(idUid, out PDAComponent? pdaComponent) || pdaComponent.ContainedID == null)
                return;

            var card = pdaComponent.ContainedID;
            _cardSystem.TryChangeFullName(card.Owner, characterName, card);
            _cardSystem.TryChangeJobTitle(card.Owner, jobPrototype.Name, card);

            var access = EntityManager.GetComponent<AccessComponent>(card.Owner);
            var accessTags = access.Tags;
            accessTags.UnionWith(jobPrototype.Access);
            _pdaSystem.SetOwner(pdaComponent, characterName);
        }
        #endregion

        private void AddManifestEntry(string characterName, string jobId)
        {
            _manifest.Add(new ManifestEntry(characterName, jobId));
        }

        #region Spawn Points
        public EntityCoordinates GetJobSpawnPoint(string jobId, StationId station)
        {
            var location = _spawnPoint;

            _possiblePositions.Clear();

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
            {
                var matchingStation =
                    EntityManager.TryGetComponent<StationComponent>(transform.ParentUid, out var stationComponent) &&
                    stationComponent.Station == station;
                DebugTools.Assert(EntityManager.TryGetComponent<IMapGridComponent>(transform.ParentUid, out _));

                if (point.SpawnType == SpawnPointType.Job && point.Job?.ID == jobId && matchingStation)
                    _possiblePositions.Add(transform.Coordinates);
            }

            if (_possiblePositions.Count != 0)
                location = _robustRandom.Pick(_possiblePositions);
            else
                location = GetLateJoinSpawnPoint(station); // We need a sane fallback here, so latejoin it is.

            return location;
        }

        public EntityCoordinates GetLateJoinSpawnPoint(StationId station)
        {
            var location = _spawnPoint;

            _possiblePositions.Clear();

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
            {
                var matchingStation =
                    EntityManager.TryGetComponent<StationComponent>(transform.ParentUid, out var stationComponent) &&
                    stationComponent.Station == station;
                DebugTools.Assert(EntityManager.TryGetComponent<IMapGridComponent>(transform.ParentUid, out _));

                if (point.SpawnType == SpawnPointType.LateJoin && matchingStation)
                    _possiblePositions.Add(transform.Coordinates);
            }

            if (_possiblePositions.Count != 0)
                location = _robustRandom.Pick(_possiblePositions);

            return location;
        }


        public EntityCoordinates GetObserverSpawnPoint()
        {
            var location = _spawnPoint;

            _possiblePositions.Clear();

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
            {
                if (point.SpawnType == SpawnPointType.Observer)
                    _possiblePositions.Add(transform.Coordinates);
            }

            if (_possiblePositions.Count != 0)
                location = _robustRandom.Pick(_possiblePositions);

            return location;
        }
        #endregion
    }

    /// <summary>
    ///     Event raised broadcast before a player is spawned by the GameTicker.
    ///     You can use this event to spawn a player off-station on late-join but also at round start.
    ///     When this event is handled, the GameTicker will not perform its own player-spawning logic.
    /// </summary>
    public sealed class PlayerBeforeSpawnEvent : HandledEntityEventArgs
    {
        public IPlayerSession Player { get; }
        public HumanoidCharacterProfile Profile { get; }
        public string? JobId { get; }
        public bool LateJoin { get; }
        public StationId Station { get; }

        public PlayerBeforeSpawnEvent(IPlayerSession player, HumanoidCharacterProfile profile, string? jobId, bool lateJoin, StationId station)
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
    public sealed class PlayerSpawnCompleteEvent : EntityEventArgs
    {
        public EntityUid Mob { get; }
        public IPlayerSession Player { get; }
        public string? JobId { get; }
        public bool LateJoin { get; }
        public StationId Station { get; }
        public HumanoidCharacterProfile Profile { get; }

        public PlayerSpawnCompleteEvent(EntityUid mob, IPlayerSession player, string? jobId, bool lateJoin, StationId station, HumanoidCharacterProfile profile)
        {
            Mob = mob;
            Player = player;
            JobId = jobId;
            LateJoin = lateJoin;
            Station = station;
            Profile = profile;
        }
    }
}
