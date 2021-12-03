using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Speech.Components;
using Content.Server.Station;
using Content.Shared.Administration.Logs;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Server.Station.StationSystem;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        private const string PlayerPrototypeName = "MobHuman";
        private const string ObserverPrototypeName = "MobObserver";

        [Dependency] private readonly IdCardSystem _cardSystem = default!;

        /// <summary>
        /// Can't yet be removed because every test ever seems to depend on it. I'll make removing this a different PR.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private EntityCoordinates _spawnPoint;

        // Mainly to avoid allocations.
        private readonly List<EntityCoordinates> _possiblePositions = new();

        private void SpawnPlayer(IPlayerSession player, StationId station, string? jobId = null, bool lateJoin = true)
        {
            var character = GetPlayerProfile(player);

            SpawnPlayer(player, character, station, jobId, lateJoin);
            UpdateJobsAvailable();
        }

        private void SpawnPlayer(IPlayerSession player, HumanoidCharacterProfile character, StationId station, string? jobId = null, bool lateJoin = true)
        {
            if (station == StationId.Invalid)
            {
                var stations = _stationSystem.StationInfo.Keys.ToList();
                _robustRandom.Shuffle(stations);
                if (stations.Count == 0)
                    station = StationId.Invalid;
                else
                    station = stations[0];
            }

            // Can't spawn players with a dummy ticker!
            if (DummyTicker)
                return;

            if (lateJoin && DisallowLateJoin)
            {
                MakeObserve(player);
                return;
            }

            // Pick best job best on prefs.
            jobId ??= PickBestAvailableJob(character, station);
            // If no job available, just bail out.
            if (jobId is null)
            {
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
            newMind.TransferTo(mob.Uid);

            if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                IoCManager.Resolve<IEntityManager>().AddComponent<OwOAccentComponent>(mob);
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
                _adminLogSystem.Add(LogType.LateJoin, LogImpact.Medium, $"Player {player.Name} late joined as {character.Name:characterName} on station {_stationSystem.StationInfo[station].Name:stationName} with {mob} as a {job.Name:jobName}.");
            else
                _adminLogSystem.Add(LogType.RoundStartJoin, LogImpact.Medium, $"Player {player.Name} joined as {character.Name:characterName} on station {_stationSystem.StationInfo[station].Name:stationName} with {mob} as a {job.Name:jobName}.");

            Preset?.OnSpawnPlayerCompleted(player, mob, lateJoin);
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

            if (!_playersInLobby.ContainsKey(player)) return;

            PlayerJoinGame(player);

            var name = GetPlayerProfile(player).Name;

            var data = player.ContentData();

            DebugTools.AssertNotNull(data);

            data!.WipeMind();
            var newMind = new Mind.Mind(data.UserId);
            newMind.ChangeOwningPlayer(data.UserId);
            newMind.AddRole(new ObserverRole(newMind));

            var mob = SpawnObserverMob();
            mob.Name = name;
            var ghost = mob.GetComponent<GhostComponent>();
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(ghost, false);
            newMind.TransferTo(mob.Uid);

            _playersInLobby[player] = LobbyPlayerStatus.Observer;
            RaiseNetworkEvent(GetStatusSingle(player, LobbyPlayerStatus.Observer));
        }

        #region Mob Spawning Helpers
        private IEntity SpawnPlayerMob(Job job, HumanoidCharacterProfile? profile, StationId station, bool lateJoin = true)
        {
            var coordinates = lateJoin ? GetLateJoinSpawnPoint(station) : GetJobSpawnPoint(job.Prototype.ID, station);
            var entity = EntityManager.SpawnEntity(PlayerPrototypeName, coordinates);

            if (job.StartingGear != null)
            {
                var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
                EquipStartingGear(entity, startingGear, profile);
            }

            if (profile != null)
            {
                EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(entity.Uid, profile);
                entity.Name = profile.Name;
            }

            return entity;
        }

        private IEntity SpawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return EntityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }
        #endregion

        #region Equip Helpers
        public void EquipStartingGear(IEntity entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
        {
            if (entity.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var slot in EquipmentSlotDefines.AllSlots)
                {
                    var equipmentStr = startingGear.GetGear(slot, profile);
                    if (!string.IsNullOrEmpty(equipmentStr))
                    {
                        var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, entity.Transform.Coordinates);
                        inventory.Equip(slot, equipmentEntity.GetComponent<ItemComponent>());
                    }
                }
            }

            if (entity.TryGetComponent(out HandsComponent? handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var (hand, prototype) in inhand)
                {
                    var inhandEntity = EntityManager.SpawnEntity(prototype, entity.Transform.Coordinates);
                    handsComponent.TryPickupEntity(hand, inhandEntity, checkActionBlocker: false);
                }
            }
        }

        public void EquipIdCard(IEntity entity, string characterName, JobPrototype jobPrototype)
        {
            if (!entity.TryGetComponent(out InventoryComponent? inventory))
                return;

            if (!inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent? item))
            {
                return;
            }

            var itemEntity = item.Owner;

            if (!itemEntity.TryGetComponent(out PDAComponent? pdaComponent) || pdaComponent.ContainedID == null)
                return;

            var card = pdaComponent.ContainedID;
            _cardSystem.TryChangeFullName(card.Owner.Uid, characterName, card);
            _cardSystem.TryChangeJobTitle(card.Owner.Uid, jobPrototype.Name, card);

            var access = card.Owner.GetComponent<AccessComponent>();
            var accessTags = access.Tags;
            accessTags.UnionWith(jobPrototype.Access);
            EntityManager.EntitySysManager.GetEntitySystem<PDASystem>()
                .SetOwner(pdaComponent, characterName);
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

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>())
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

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>())
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

            foreach (var (point, transform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>())
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
}
