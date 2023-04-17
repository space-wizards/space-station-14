using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

public sealed class SpawnSalvageMissionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ITileDefinitionManager _tileDefManager;
    private readonly BiomeSystem _biome;
    private readonly DungeonSystem _dungeon;
    private readonly SalvageSystem _salvage;

    public readonly EntityUid Station;
    private readonly SalvageMissionParams _missionParams;

    public SpawnSalvageMissionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        ITileDefinitionManager tileDefManager,
        BiomeSystem biome,
        DungeonSystem dungeon,
        SalvageSystem salvage,
        EntityUid station,
        SalvageMissionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _tileDefManager = tileDefManager;
        _biome = biome;
        _dungeon = dungeon;
        _salvage = salvage;
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

        var missionBiome = _prototypeManager.Index<SalvageBiomeMod>(mission.Biome);

        if (missionBiome.BiomePrototype != null)
        {
            var biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetPrototype(biome, mission.Biome);
            biomeSystem.SetSeed(biome, mission.Seed);
            _entManager.Dirty(biome);

            // Gravity
            var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
            gravity.Enabled = true;
            _entManager.Dirty(gravity, metadata);

            // Atmos
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

            if (mission.Color != null)
            {
                var lighting = _entManager.EnsureComponent<MapLightComponent>(mapUid);
                lighting.AmbientLightColor = mission.Color.Value;
                _entManager.Dirty(lighting);
            }
        }

        _mapManager.DoMapInitialize(mapId);
        _mapManager.SetMapPaused(mapId, true);

        // Setup expedition
        var expedition = _entManager.AddComponent<SalvageExpeditionComponent>(mapUid);
        expedition.Station = Station;
        expedition.EndTime = _timing.CurTime + mission.Duration;
        expedition.MissionParams = _missionParams;

        // Don't want consoles to have the incorrect name until refreshed.
        var ftlUid = _entManager.CreateEntityUninitialized("FTLPoint", new EntityCoordinates(mapUid, Vector2.Zero));
        _entManager.GetComponent<MetaDataComponent>(ftlUid).EntityName = SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>(config.NameProto), _missionParams.Seed);
        _entManager.InitializeAndStartEntity(ftlUid);

        var landingPadRadius = 24;
        var minDungeonOffset = landingPadRadius + 12;

        var dungeonRotation = _dungeon.GetDungeonRotation(_missionParams.Seed);
        var dungeonSpawnRotation = new Angle(random.NextDouble() * Math.Tau);

        // If the dungeon were to spawn facing the landing pad then bump the offset a bit
        // This isn't robust but fine for now.
        if ((dungeonRotation - dungeonSpawnRotation).Reduced() > Math.PI / 2)
        {
            minDungeonOffset += 16;
        }

        var maxDungeonOffset = minDungeonOffset + 24;
        var dungeonOffsetDistance = minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat();
        var dungeonOffset = new Vector2(dungeonOffsetDistance, 0f);
        dungeonOffset = dungeonSpawnRotation.RotateVec(dungeonOffset);
        var dungeonConfig = _prototypeManager.Index<DungeonConfigPrototype>(mission.Dungeon);
        var dungeon =
            await WaitAsyncTask(_dungeon.GenerateDungeonAsync(dungeonConfig, mapUid, grid, (Vector2i) dungeonOffset,
                _missionParams.Seed));

        // Aborty
        if (dungeon.Rooms.Count == 0)
        {
            return false;
        }

        List<Vector2i> reservedTiles = new();

        // Setup the landing pad
        var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
        var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);

        // Set the tiles themselves
        var seed = new FastNoiseLite(_missionParams.Seed);
        var landingTile = new Tile(_tileDefManager["FloorSteel"].TileId);

        foreach (var tile in grid.GetTilesIntersecting(new Circle(Vector2.Zero, landingPadRadius), false))
        {
            if (!_biome.TryGetBiomeTile(mapUid, grid, seed, tile.GridIndices, out _))
                continue;

            tiles.Add((tile.GridIndices, landingTile));
            reservedTiles.Add(tile.GridIndices);
        }

        grid.SetTiles(tiles);

        await SetupMission(mission.Mission, mission, (Vector2i) dungeonOffset, dungeon, mapUid, grid, random);

        // Handle loot
        foreach (var loot in mission.Loot)
        {
            var lootProto = _prototypeManager.Index<SalvageLootPrototype>(loot);
            await SpawnDungeonLoot(dungeon, lootProto, mapUid, grid, random, reservedTiles);

        }
        return true;
    }

    private async Task SpawnDungeonLoot(Dungeon dungeon, SalvageLootPrototype loot, EntityUid gridUid, MapGridComponent grid, Random random, List<Vector2i> reservedTiles)
    {
        foreach (var rule in loot.LootRules)
        {
            switch (rule)
            {
                case BiomeLoot biome:
                    break;
                // Spawns a cluster (like an ore vein) nearby.
                case ClusterLoot cluster:
                    await SpawnClusterLoot(dungeon, cluster, gridUid, grid, random, reservedTiles);
                    break;
            }
        }
    }

    #region Loot

    private async Task SpawnClusterLoot(
        Dungeon dungeon,
        ClusterLoot loot,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random,
        List<Vector2i> reservedTiles)
    {
        var spawnTiles = new HashSet<Vector2i>();
        var dungeonCenter = dungeon.Center;
        // TODO: More robust
        var minRadius = 16f;
        var maxRadius = 32f;

        for (var i = 0; i < loot.Points; i++)
        {
            var distance = minRadius + (maxRadius - minRadius) * random.NextFloat();
            var angle = new Angle(random.NextDouble() * Math.Tau);
            var offset = angle.RotateVec(new Vector2(distance, 0f));
            var spawnOrigin = dungeon.Center + (Vector2i) offset;

            // Spread out from the wall
            var frontier = new List<Vector2i> {spawnOrigin};
            var clusterAmount = random.Next(loot.MinClusterAmount, loot.MaxClusterAmount);

            for (var j = 0; j < clusterAmount; j++)
            {
                if (frontier.Count == 0)
                    break;

                var nodeIndex = random.Next(frontier.Count);
                var node = frontier[nodeIndex];
                frontier.RemoveSwap(nodeIndex);

                if (reservedTiles.Contains(node))
                    continue;

                var anchored = grid.GetAnchoredEntitiesEnumerator(node);

                for (var k = 0; k < 4; k++)
                {
                    var direction = (Direction) (k * 2);
                    var neighbor = node + direction.ToIntVec();

                    frontier.Add(neighbor);
                }

                if (!anchored.MoveNext(out _))
                {
                    spawnTiles.Add(node);
                }
            }
        }

        foreach (var tile in spawnTiles)
        {
            await SuspendIfOutOfTime();
            _entManager.SpawnEntity(loot.Prototype, grid.GridTileToLocal(tile));
        }
    }

    #endregion

    #region Mission Specific

    private async Task SetupMission(string missionMod, SalvageMission mission, Vector2i dungeonOffset, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        switch (missionMod)
        {
        case "Mining":
            await SetupMining(mission, dungeon, gridUid, grid, random);
            return;
        case "StructureDestroy":
            await SetupStructure(mission, dungeon, gridUid, grid, random);
            return;
        default:
            throw new NotImplementedException();
        }
    }

    private async Task SetupMining(
        SalvageMission mission,
        Dungeon dungeon,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random)
    {

    }

    private async Task SetupStructure(
        SalvageMission mission,
        Dungeon dungeon,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random)
    {
        var structureComp = _entManager.EnsureComponent<SalvageStructureExpeditionComponent>(gridUid);
        var availableRooms = dungeon.Rooms.ToList();
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        await SpawnMobsRandomRooms(mission, dungeon, faction, grid, random);

        var structureCount = _salvage.GetStructureCount(mission.Difficulty);
        var shaggy = faction.Configs["DefenseStructure"];

        // Spawn the objectives
        for (var i = 0; i < structureCount; i++)
        {
            var structureRoom = availableRooms[random.Next(availableRooms.Count)];
            var spawnTile = structureRoom.Tiles.ElementAt(random.Next(structureRoom.Tiles.Count));
            var uid = _entManager.SpawnEntity(shaggy, grid.GridTileToLocal(spawnTile));
            _entManager.AddComponent<SalvageStructureComponent>(uid);
            structureComp.Structures.Add(uid);
        }
    }

    private async Task SpawnMobsRandomRooms(SalvageMission mission, Dungeon dungeon, SalvageFactionPrototype faction, MapGridComponent grid, Random random)
    {
        var groupSpawns = _salvage.GetSpawnCount(mission.Difficulty, mission.RemainingDifficulty);
        var groupSum = faction.MobGroups.Sum(o => o.Prob);

        for (var i = 0; i < groupSpawns; i++)
        {
            var roll = random.NextFloat() * groupSum;
            var value = 0f;

            foreach (var group in faction.MobGroups)
            {
                value += group.Prob;

                if (value < roll)
                    continue;

                var mobGroupIndex = random.Next(faction.MobGroups.Count);
                var mobGroup = faction.MobGroups[mobGroupIndex];

                var spawnRoomIndex = random.Next(dungeon.Rooms.Count);
                var spawnRoom = dungeon.Rooms[spawnRoomIndex];
                var spawnTile = spawnRoom.Tiles.ElementAt(random.Next(spawnRoom.Tiles.Count));
                var spawnPosition = grid.GridTileToLocal(spawnTile);

                foreach (var entry in EntitySpawnCollection.GetSpawns(mobGroup.Entries, random))
                {
                    _entManager.SpawnEntity(entry, spawnPosition);
                }

                await SuspendIfOutOfTime();
                break;
            }
        }
    }

    #endregion
}
