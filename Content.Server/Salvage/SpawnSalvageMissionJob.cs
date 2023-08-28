using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Atmos;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Dataset;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Salvage;

public sealed class SpawnSalvageMissionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly AnchorableSystem _anchorable;
    private readonly BiomeSystem _biome;
    private readonly DungeonSystem _dungeon;
    private readonly MetaDataSystem _metaData;
    private readonly SalvageSystem _salvage;

    public readonly EntityUid Station;
    private readonly SalvageMissionParams _missionParams;

    public SpawnSalvageMissionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        AnchorableSystem anchorable,
        BiomeSystem biome,
        DungeonSystem dungeon,
        MetaDataSystem metaData,
        SalvageSystem salvage,
        EntityUid station,
        SalvageMissionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _anchorable = anchorable;
        _biome = biome;
        _dungeon = dungeon;
        _metaData = metaData;
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
        BiomeComponent? biome = null;

        if (missionBiome.BiomePrototype != null)
        {
            biome = _entManager.AddComponent<BiomeComponent>(mapUid);
            var biomeSystem = _entManager.System<BiomeSystem>();
            biomeSystem.SetTemplate(biome, _prototypeManager.Index<BiomeTemplatePrototype>(missionBiome.BiomePrototype));
            biomeSystem.SetSeed(biome, mission.Seed);
            _entManager.Dirty(biome);

            // Gravity
            var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
            gravity.Enabled = true;
            _entManager.Dirty(gravity, metadata);

            // Atmos
            var air = _prototypeManager.Index<SalvageAirMod>(mission.Air);
            // copy into a new array since the yml deserialization discards the fixed length
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            air.Gases.CopyTo(moles, 0);
            var atmos = _entManager.EnsureComponent<MapAtmosphereComponent>(mapUid);
            _entManager.System<AtmosphereSystem>().SetMapSpace(mapUid, air.Space, atmos);
            _entManager.System<AtmosphereSystem>().SetMapGasMixture(mapUid, new GasMixture(2500)
            {
                Temperature = mission.Temperature,
                Moles = moles,
            }, atmos);

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
        expedition.Difficulty = _missionParams.Difficulty;
        expedition.Rewards = mission.Rewards;

        // Don't want consoles to have the incorrect name until refreshed.
        var ftlUid = _entManager.CreateEntityUninitialized("FTLPoint", new EntityCoordinates(mapUid, grid.TileSizeHalfVector));
        _metaData.SetEntityName(ftlUid, SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _missionParams.Seed));
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

        foreach (var tile in grid.GetTilesIntersecting(new Circle(Vector2.Zero, landingPadRadius), false))
        {
            if (!_biome.TryGetBiomeTile(mapUid, grid, tile.GridIndices, out _))
                continue;

            reservedTiles.Add(tile.GridIndices);
        }

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
        // We'll always add this loot if possible
        foreach (var lootProto in _prototypeManager.EnumeratePrototypes<SalvageLootPrototype>())
        {
            if (!lootProto.Guaranteed)
                continue;

            await SpawnDungeonLoot(dungeon, missionBiome, lootProto, mapUid, grid, random, reservedTiles);
        }

        return true;
    }

    private async Task SpawnDungeonLoot(Dungeon? dungeon, SalvageBiomeMod biomeMod, SalvageLootPrototype loot, EntityUid gridUid, MapGridComponent grid, Random random, List<Vector2i> reservedTiles)
    {
        for (var i = 0; i < loot.LootRules.Count; i++)
        {
            var rule = loot.LootRules[i];

            switch (rule)
            {
                case BiomeMarkerLoot biomeLoot:
                    {
                        if (_entManager.TryGetComponent<BiomeComponent>(gridUid, out var biome) &&
                            biomeLoot.Prototype.TryGetValue(biomeMod.ID, out var mod))
                        {
                            _biome.AddMarkerLayer(biome, mod);
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
            }
        }
    }

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
        var validSpawns = new List<Vector2i>();

        // Spawn the objectives
        for (var i = 0; i < structureCount; i++)
        {
            var structureRoom = availableRooms[random.Next(availableRooms.Count)];
            validSpawns.Clear();
            validSpawns.AddRange(structureRoom.Tiles);
            random.Shuffle(validSpawns);

            while (validSpawns.Count > 0)
            {
                var spawnTile = validSpawns[^1];
                validSpawns.RemoveAt(validSpawns.Count - 1);

                if (!_anchorable.TileFree(grid, spawnTile, (int) CollisionGroup.MachineLayer,
                        (int) CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var spawnPosition = grid.GridTileToLocal(spawnTile);
                var uid = _entManager.SpawnEntity(shaggy, spawnPosition);
                _entManager.AddComponent<SalvageStructureComponent>(uid);
                structureComp.Structures.Add(uid);
                break;
            }
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
        var validSpawns = new List<Vector2i>();

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
                validSpawns.Clear();
                validSpawns.AddRange(spawnRoom.Tiles);
                random.Shuffle(validSpawns);

                foreach (var entry in EntitySpawnCollection.GetSpawns(mobGroup.Entries, random))
                {
                    while (validSpawns.Count > 0)
                    {
                        var spawnTile = validSpawns[^1];
                        validSpawns.RemoveAt(validSpawns.Count - 1);

                        if (!_anchorable.TileFree(grid, spawnTile, (int) CollisionGroup.MachineLayer,
                                (int) CollisionGroup.MachineLayer))
                        {
                            continue;
                        }

                        var spawnPosition = grid.GridTileToLocal(spawnTile);

                        var uid = _entManager.CreateEntityUninitialized(entry, spawnPosition);
                        _entManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                        _entManager.RemoveComponent<GhostRoleComponent>(uid);
                        _entManager.InitializeAndStartEntity(uid);

                        break;
                    }
                }

                await SuspendIfOutOfTime();
                break;
            }
        }
    }

    #endregion
}
