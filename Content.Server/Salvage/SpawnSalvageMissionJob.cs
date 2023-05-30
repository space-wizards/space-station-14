using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
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
        var config = _missionParams.MissionType;
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _mapManager.AddUninitializedMap(mapId);
        MetaDataComponent? metadata = null;
        var grid = _entManager.EnsureComponent<MapGridComponent>(mapUid);
        var random = new Random(_missionParams.Seed);

        // Setup mission configs
        // As we go through the config the rating will deplete so we'll go for most important to least important.

        var mission = _entManager.System<SharedSalvageSystem>()
            .GetMission(_missionParams.MissionType, _missionParams.Difficulty, _missionParams.Seed);

        var missionBiome = _prototypeManager.Index<SalvageBiomeMod>(mission.Biome);

        if (missionBiome.BiomePrototype != null)
        {
            var biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetTemplate(biome, _prototypeManager.Index<BiomeTemplatePrototype>(missionBiome.BiomePrototype));
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
        _entManager.GetComponent<MetaDataComponent>(ftlUid).EntityName = SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _missionParams.Seed);
        _entManager.InitializeAndStartEntity(ftlUid);

        var landingPadRadius = 24;
        var minDungeonOffset = landingPadRadius + 4;

        // We'll use the dungeon rotation as the spawn angle
        var dungeonRotation = _dungeon.GetDungeonRotation(_missionParams.Seed);

        Dungeon dungeon = default!;

        if (config != SalvageMissionType.Mining)
        {
            var maxDungeonOffset = minDungeonOffset + 12;
            var dungeonOffsetDistance = minDungeonOffset + (maxDungeonOffset - minDungeonOffset) * random.NextFloat();
            var dungeonOffset = new Vector2(0f, dungeonOffsetDistance);
            dungeonOffset = dungeonRotation.RotateVec(dungeonOffset);
            var dungeonMod = _prototypeManager.Index<SalvageDungeonMod>(mission.Dungeon);
            var dungeonConfig = _prototypeManager.Index<DungeonConfigPrototype>(dungeonMod.Proto);
            dungeon =
                await WaitAsyncTask(_dungeon.GenerateDungeonAsync(dungeonConfig, mapUid, grid, (Vector2i) dungeonOffset,
                    _missionParams.Seed));

            // Aborty
            if (dungeon.Rooms.Count == 0)
            {
                return false;
            }

            expedition.DungeonLocation = dungeonOffset;
        }

        List<Vector2i> reservedTiles = new();

        // Setup the landing pad
        var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
        var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);

        // Set the tiles themselves
        var landingTile = new Tile(_tileDefManager["FloorSteel"].TileId);

        foreach (var tile in grid.GetTilesIntersecting(new Circle(Vector2.Zero, landingPadRadius), false))
        {
            if (!_biome.TryGetBiomeTile(mapUid, grid, tile.GridIndices, out _))
                continue;

            tiles.Add((tile.GridIndices, landingTile));
            reservedTiles.Add(tile.GridIndices);
        }

        grid.SetTiles(tiles);

        // Mission setup
        switch (config)
        {
            case SalvageMissionType.Mining:
                await SetupMining(mission, mapUid);
                break;
            case SalvageMissionType.Destruction:
                await SetupStructure(mission, dungeon, mapUid, grid, random);
                break;
            case SalvageMissionType.Elimination:
                await SetupElimination(mission, dungeon, mapUid, grid, random);
                break;
            default:
                throw new NotImplementedException();
        }

        // Handle loot
        foreach (var (loot, count) in mission.Loot)
        {
            for (var i = 0; i < count; i++)
            {
                var lootProto = _prototypeManager.Index<SalvageLootPrototype>(loot);
                await SpawnDungeonLoot(dungeon, lootProto, mapUid, grid, random, reservedTiles);
            }
        }
        return true;
    }

    private async Task SpawnDungeonLoot(Dungeon? dungeon, SalvageLootPrototype loot, EntityUid gridUid, MapGridComponent grid, Random random, List<Vector2i> reservedTiles)
    {
        for (var i = 0; i < loot.LootRules.Count; i++)
        {
            var rule = loot.LootRules[i];

            switch (rule)
            {
                case BiomeMarkerLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
                        {
                            _biome.AddMarkerLayer(biome, biomeLoot.Prototype);
                        }
                    }
                    break;
                case BiomeTemplateLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
                        {
                            _biome.AddTemplate(biome, "Loot", _prototypeManager.Index<BiomeTemplatePrototype>(biomeLoot.Prototype), i);
                        }
                    }
                    break;
                // Spawns a cluster (like an ore vein) nearby.
                case DungeonClusterLoot clusterLoot:
                    await SpawnDungeonClusterLoot(dungeon!, clusterLoot, grid, random, reservedTiles);
                    break;
            }
        }
    }

    #region Loot

    private async Task SpawnDungeonClusterLoot(
        Dungeon dungeon,
        DungeonClusterLoot loot,
        MapGridComponent grid,
        Random random,
        List<Vector2i> reservedTiles)
    {
        var spawnTiles = new HashSet<Vector2i>();

        for (var i = 0; i < loot.Points; i++)
        {
            var room = dungeon.Rooms[random.Next(dungeon.Rooms.Count)];
            var clusterAmount = loot.ClusterAmount;
            var spots = room.Tiles.ToList();
            random.Shuffle(spots);

            foreach (var spot in spots)
            {
                if (reservedTiles.Contains(spot))
                    continue;

                var anchored = grid.GetAnchoredEntitiesEnumerator(spot);

                if (anchored.MoveNext(out _))
                {
                    continue;
                }

                clusterAmount--;
                spawnTiles.Add(spot);

                if (clusterAmount == 0)
                    break;
            }
        }

        foreach (var tile in spawnTiles)
        {
            await SuspendIfOutOfTime();
            var proto = _prototypeManager.Index<WeightedRandomPrototype>(loot.Prototype).Pick(random);
            _entManager.SpawnEntity(proto, grid.GridTileToLocal(tile));
        }
    }

    #endregion

    #region Mission Specific

    private async Task SetupMining(
        SalvageMission mission,
        EntityUid gridUid)
    {
        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);

        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome))
        {
            // TODO: Better
            for (var i = 0; i < _salvage.GetDifficulty(mission.Difficulty); i++)
            {
                _biome.AddMarkerLayer(biome, faction.Configs["Mining"]);
            }
        }
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

    private async Task SetupElimination(
        SalvageMission mission,
        Dungeon dungeon,
        EntityUid gridUid,
        MapGridComponent grid,
        Random random)
    {
        // spawn megafauna in a random place
        var roomIndex = random.Next(dungeon.Rooms.Count);
        var room = dungeon.Rooms[roomIndex];
        var tile = room.Tiles.ElementAt(random.Next(room.Tiles.Count));
        var position = grid.GridTileToLocal(tile);

        var faction = _prototypeManager.Index<SalvageFactionPrototype>(mission.Faction);
        var prototype = faction.Configs["Megafauna"];
        var uid = _entManager.SpawnEntity(prototype, position);
        // not removing ghost role since its 1 megafauna, expect that you won't be able to cheese it.
        var eliminationComp = _entManager.EnsureComponent<SalvageEliminationExpeditionComponent>(gridUid);
        eliminationComp.Megafauna.Add(uid);

        // spawn less mobs than usual since there's megafauna to deal with too
        await SpawnMobsRandomRooms(mission, dungeon, faction, grid, random, 0.5f);
    }

    private async Task SpawnMobsRandomRooms(SalvageMission mission, Dungeon dungeon, SalvageFactionPrototype faction, MapGridComponent grid, Random random, float scale = 1f)
    {
        // scale affects how many groups are spawned, not the size of the groups themselves
        var groupSpawns = _salvage.GetSpawnCount(mission.Difficulty) * scale;
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
                    var uid = _entManager.CreateEntityUninitialized(entry, spawnPosition);
                    _entManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                    _entManager.RemoveComponent<GhostRoleComponent>(uid);
                    _entManager.InitializeAndStartEntity(uid);
                }

                await SuspendIfOutOfTime();
                break;
            }
        }
    }

    #endregion
}
