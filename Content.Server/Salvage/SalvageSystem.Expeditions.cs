using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private const int MissionLimit = 5;

    private readonly JobQueue _salvageQueue = new();
    private List<(SpawnSalvageMissionJob Job, CancellationTokenSource CancelToken)> _salvageJobs = new();
    private const double SalvageJobTime = 0.002;

    private void InitializeExpeditions()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnSalvageExpStationInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ComponentInit>(OnSalvageConsoleInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, EntParentChangedMessage>(OnSalvageConsoleParent);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ClaimSalvageMessage>(OnSalvageClaimMessage);

        SubscribeLocalEvent<SalvageExpeditionDataComponent, EntityUnpausedEvent>(OnDataUnpaused);

        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentShutdown>(OnExpeditionShutdown);
        SubscribeLocalEvent<SalvageExpeditionComponent, EntityUnpausedEvent>(OnExpeditionUnpaused);
    }

    private void OnExpeditionShutdown(EntityUid uid, SalvageExpeditionComponent component, ComponentShutdown args)
    {
        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            if (job.Station == component.Station)
            {
                cancelToken.Cancel();
                _salvageJobs.Remove((job, cancelToken));
            }
        }

        // Finish mission
        if (TryComp<SalvageExpeditionDataComponent>(component.Station, out var data))
        {
            FinishExpedition(data, component);
        }
    }

    private void OnDataUnpaused(EntityUid uid, SalvageExpeditionDataComponent component, ref EntityUnpausedEvent args)
    {
        component.NextOffer += args.PausedTime;
    }

    private void OnExpeditionUnpaused(EntityUid uid, SalvageExpeditionComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    private void OnSalvageExpStationInit(StationInitializedEvent ev)
    {
        EnsureComp<SalvageExpeditionDataComponent>(ev.Station);
    }

    private void UpdateExpeditions()
    {
        var currentTime = _timing.CurTime;
        _salvageQueue.Process();

        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            switch (job.Status)
            {
                case JobStatus.Finished:
                    _salvageJobs.Remove((job, cancelToken));
                    break;
            }
        }

        foreach (var comp in EntityQuery<SalvageExpeditionDataComponent>())
        {
            // Update offers
            if (comp.NextOffer > currentTime)
                continue;

            // Were we on cooldown.
            if (comp.Claimed)
            {
                comp.ActiveMission = 0;
            }

            comp.NextOffer += MissionCooldown;
            GenerateMissions(comp);
            UpdateConsoles(comp);
        }

        foreach (var comp in EntityQuery<SalvageExpeditionComponent>())
        {
            if (comp.EndTime < currentTime)
            {
                QueueDel(comp.Owner);
            }
        }
    }

    private void FinishExpedition(SalvageExpeditionDataComponent component, SalvageExpeditionComponent expedition)
    {
        // Payout already handled elsewhere.
        if (expedition.Completed)
        {
            _sawmill.Debug($"Completed mission {expedition.MissionParams.Config} with seed {expedition.MissionParams.Seed}");
            component.NextOffer = _timing.CurTime + MissionCooldown;
        }
        else
        {
            _sawmill.Debug($"Failed mission {expedition.MissionParams.Config} with seed {expedition.MissionParams.Seed}");
            component.NextOffer = _timing.CurTime + MissionFailedCooldown;
        }

        UpdateConsoles(component);

    }

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.Missions.Clear();
        var configs = _prototypeManager.EnumeratePrototypes<SalvageMissionPrototype>().ToArray();

        if (configs.Length == 0)
            return;

        for (var i = 0; i < MissionLimit; i++)
        {
            var config = _random.Pick(configs);

            var mission = new SalvageMissionParams()
            {
                Index = component.NextIndex,
                Config = config.ID,
                Seed = _random.Next(),
                Difficulty = (DifficultyRating) i,
            };

            component.Missions[component.NextIndex++] = mission;
        }
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.NextOffer, component.Claimed, component.ActiveMission, missions);
    }

    private void SpawnMission(SalvageMissionParams missionParams, EntityUid station)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new SpawnSalvageMissionJob(
            SalvageJobTime,
            EntityManager,
            _timing,
            _mapManager,
            _prototypeManager,
            _biome,
            _dungeon,
            station,
            missionParams,
            cancelToken.Token);

        _salvageJobs.Add((job, cancelToken));
        _salvageQueue.EnqueueJob(job);
    }

    private sealed class SpawnSalvageMissionJob : Job<bool>
    {
        private readonly IEntityManager _entManager;
        private readonly IGameTiming _timing;
        private readonly IMapManager _mapManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly BiomeSystem _biome;
        private readonly DungeonSystem _dungeon;

        public readonly EntityUid Station;
        private readonly SalvageMissionParams _missionParams;

        public SpawnSalvageMissionJob(
            double maxTime,
            IEntityManager entManager,
            IGameTiming timing,
            IMapManager mapManager,
            IPrototypeManager protoManager,
            BiomeSystem biome,
            DungeonSystem dungeon,
            EntityUid station,
            SalvageMissionParams missionParams,
            CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
            _entManager = entManager;
            _timing = timing;
            _mapManager = mapManager;
            _prototypeManager = protoManager;
            _biome = biome;
            _dungeon = dungeon;
            Station = station;
            _missionParams = missionParams;
        }

        protected override async Task<bool> Process()
        {
            Logger.DebugS("salvage", $"Spawning salvage mission with seed {_missionParams.Seed}");
            var config = _prototypeManager.Index<SalvageMissionPrototype>(_missionParams.Config);
            var mapId = _mapManager.CreateMap();
            var mapUid = _mapManager.GetMapEntityId(mapId);
            _mapManager.AddUninitializedMap(mapId);
            MetaDataComponent? metadata = null;
            var grid = _entManager.EnsureComponent<MapGridComponent>(mapUid);
            var random = new Random(_missionParams.Seed);

            // Setup mission configs
            // As we go through the config the rating will deplete so we'll go for most important to least important.

            var mission = _entManager.System<SharedSalvageSystem>()
                .GetMission(_missionParams.Config, _missionParams.Difficulty, _missionParams.Seed);

            if (mission.Biome != null)
            {
                var biome = _entManager.AddComponent<BiomeComponent>(mapUid);
                var biomeSystem = _entManager.System<BiomeSystem>();
                biomeSystem.SetPrototype(biome, mission.Biome);
                biomeSystem.SetSeed(biome, mission.Seed);
                _entManager.Dirty(biome);
            }

            if (mission.Color != null)
            {
                var lighting = _entManager.EnsureComponent<MapLightComponent>(mapUid);
                lighting.AmbientLightColor = mission.Color.Value;
                _entManager.Dirty(lighting);
            }

            if (true)//mission.Gravity)
            {
                var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
                gravity.Enabled = true;
                _entManager.Dirty(gravity, metadata);
            }

            if (true)//mission.Atmos)
            {
                var atmos = _entManager.EnsureComponent<MapAtmosphereComponent>(mapUid);
                atmos.Space = false;
                var moles = new float[Atmospherics.AdjustedNumberOfGases];
                moles[(int) Gas.Oxygen] = 21.824779f;
                moles[(int) Gas.Nitrogen] = 82.10312f;

                atmos.Mixture = new GasMixture(2500)
                {
                    Temperature = 293.15f,
                    Moles = moles,
                };
            }

            _mapManager.DoMapInitialize(mapId);
            _mapManager.SetMapPaused(mapId, true);

            // Setup expedition
            var expedition = _entManager.AddComponent<SalvageExpeditionComponent>(mapUid);
            expedition.Station = Station;
            expedition.EndTime = _timing.CurTime + mission.Duration;
            expedition.MissionParams = _missionParams;

            var ftlUid = _entManager.SpawnEntity("FTLPoint", new EntityCoordinates(mapUid, Vector2.Zero));
            _entManager.GetComponent<MetaDataComponent>(ftlUid).EntityName = GetFTLName(_prototypeManager.Index<DatasetPrototype>(config.NameProto), _missionParams.Seed);

            var landingPadRadius = 24;
            var minDungeonOffset = landingPadRadius + 32;
            var maxDungeonOffset = minDungeonOffset + 32;

            var dungeonOffsetDistance = (minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat());
            var dungeonOffset = new Vector2(dungeonOffsetDistance, 0f);
            dungeonOffset = new Angle(random.NextDouble() * Math.Tau).RotateVec(dungeonOffset);
            var dungeonConfig = _prototypeManager.Index<DungeonConfigPrototype>(mission.Dungeon);
            var dungeon = await _dungeon.GenerateDungeonAsync(dungeonConfig, mapUid, grid, Vector2.Zero, _missionParams.Seed);

            // Aborty
            if (dungeon.Rooms.Count == 0)
            {
                return false;
            }

            // Handle loot
            /*
            foreach (var loot in GetLoot(config.Loots, missionSeed, _prototypeManager))
            {
                // await SpawnDungeonLoot(dungeonOffset, dungeon, loot, grid, random, reservedTiles);
            }
            */

            // Setup the landing pad
            var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
            var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);

            // Set the tiles themselves
            var seed = new FastNoiseLite(_missionParams.Seed);

            foreach (var tile in grid.GetTilesIntersecting(new Circle(Vector2.Zero, landingPadRadius), false))
            {
                if (!_biome.TryGetBiomeTile(mapUid, grid, seed, tile.GridIndices, out var tileRef))
                    continue;

                // TODO: Force load API or smth
                tiles.Add((tile.GridIndices, tileRef.Value));
            }

            grid.SetTiles(tiles);

            await SetupMission(mission.Mission, mission, (Vector2i) dungeonOffset, dungeon, grid, random, seed.GetSeed());
            return true;
        }

        #region Mission Specific

        private async Task SetupMission(string missionMod, SalvageMission mission, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random, int seed)
        {
            switch (missionMod)
            {
                // TODO:
                default:
                    return;
            }
        }

        /*
        private async Task SetupMission(SalvageMissionPrototype config, SalvageStructure structure, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random, int seed)
        {
            var structureComp = _entManager.GetComponent<SalvageStructureExpeditionComponent>(grid.Owner);
            // TODO: Uhh difficulty selection
            // TODO: Hardcoding
            var structureCount = GetStructureCount(structure, seed);
            var availableRooms = dungeon.Rooms.ToList();
            var faction = _prototypeManager.Index<SalvageFactionPrototype>(GetFaction(config.Factions, seed));
            // TODO: DETERMINE DEEZ NUTS
            var robusty = IoCManager.Resolve<IRobustRandom>();

            // TODO: More spawn config shit
            for (var i = 0; i < 3; i++)
            {
                var mobGroupIndex = random.Next(faction.MobGroups.Count);
                var mobGroup = faction.MobGroups[mobGroupIndex];

                var spawnRoomIndex = random.Next(dungeon.Rooms.Count);
                var spawnRoom = dungeon.Rooms[spawnRoomIndex];
                var spawnTile = spawnRoom.Tiles.ElementAt(random.Next(spawnRoom.Tiles.Count));
                spawnTile += dungeonOffset;
                var spawnPosition = grid.GridTileToLocal(spawnTile);

                foreach (var entry in EntitySpawnCollection.GetSpawns(mobGroup.Entries, robusty))
                {
                    await SuspendIfOutOfTime();
                    _entManager.SpawnEntity(entry, spawnPosition);
                }
            }

            var shaggy = (SalvageStructureFaction) faction.Configs[config.ID];

            // Spawn the objectives
            for (var i = 0; i < structureCount; i++)
            {
                var structureRoom = availableRooms[random.Next(availableRooms.Count)];
                var spawnTile = structureRoom.Tiles.ElementAt(random.Next(structureRoom.Tiles.Count)) + dungeonOffset;
                await SuspendIfOutOfTime();
                var uid = _entManager.SpawnEntity(shaggy.Spawn, grid.GridTileToLocal(spawnTile));
                structureComp.Structures.Add(uid);
            }
        }
        */

        #endregion
    }
}
