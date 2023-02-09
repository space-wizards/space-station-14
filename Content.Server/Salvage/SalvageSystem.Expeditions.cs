using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions.Extraction;
using Content.Shared.Salvage.Expeditions.Structure;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Random;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private const int MissionLimit = 5;

    private readonly JobQueue _salvageQueue = new();

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
        // Finish mission
        if (TryComp<SalvageExpeditionDataComponent>(component.Station, out var data))
        {
            FinishExpedition(data);
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

        foreach (var comp in EntityQuery<SalvageExpeditionDataComponent>())
        {
            // Update offers
            if (comp.NextOffer >= currentTime)
                continue;

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

        _salvageQueue.Process();
    }

    private void FinishExpedition(SalvageExpeditionDataComponent component)
    {
        component.ActiveMission = 0;
        component.NextOffer = _timing.CurTime + MissionCooldown;
        component.MissionCompleted = false;
        UpdateConsoles(component);
    }

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.Missions.Clear();
        const int timeBlock = 15;
        var configs = _prototypeManager.EnumeratePrototypes<SalvageExpeditionPrototype>().ToArray();

        if (configs.Length == 0)
            return;

        // TODO: sealed record
        for (var i = 0; i < MissionLimit; i++)
        {
            var config = _random.Pick(configs);
            var minTime = config.MinDuration.TotalSeconds;
            var maxTime = config.MaxDuration.TotalSeconds;

            var mission = new SalvageMission()
            {
                Index = component.NextIndex,
                Config = config.ID,
                Seed = _random.Next(),
                Duration = TimeSpan.FromSeconds(Math.Round((_random.NextDouble() * (maxTime - minTime) + minTime) / timeBlock) * timeBlock),
            };

            component.Missions[component.NextIndex++] = mission;
        }
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.NextOffer, component.Claimed, component.ActiveMission, missions);
    }

    private void SpawnMission(SalvageMission mission, EntityUid station)
    {
        Logger.DebugS("salvage", $"Spawning salvage mission with seed {mission.Seed}");
        var config = _prototypeManager.Index<SalvageExpeditionPrototype>(mission.Config);
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _mapManager.AddUninitializedMap(mapId);
        MetaDataComponent? metadata = null;
        var grid = EnsureComp<MapGridComponent>(_mapManager.GetMapEntityId(mapId));

        // Setup mission configs
        var biome = EnsureComp<BiomeComponent>(mapUid);
        biome.BiomePrototype = config.Biome;
        var biomeProto = _prototypeManager.Index<BiomePrototype>(config.Biome);
        Dirty(biome);

        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        Dirty(gravity, metadata);

        var lighting = EnsureComp<MapLightComponent>(mapUid);
        lighting.AmbientLightColor = config.Light;
        Dirty(lighting);

        var atmos = EnsureComp<MapAtmosphereComponent>(mapUid);
        atmos.Space = false;
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        atmos.Mixture = new GasMixture(2500)
        {
            Temperature = config.Temperature,
            Moles = moles,
        };

        _mapManager.DoMapInitialize(mapId);

        // No point raising an event for this when it's 1-1.
        // TODO: Fix the landingfloor radius shenanigans
        var missionSeed = mission.Seed;
        var random = new Random(missionSeed);

        // Setup expedition
        var expedition = AddComp<SalvageExpeditionComponent>(mapUid);
        expedition.Station = station;
        expedition.EndTime = _timing.CurTime + mission.Duration;
        expedition.Faction = config.Factions[random.Next(config.Factions.Count)];
        expedition.Config = config.ID;

        var ftlUid = Spawn("FTLPoint", new EntityCoordinates(mapUid, Vector2.Zero));
        MetaData(ftlUid).EntityName = "Unga bunga";

        switch (config.Expedition)
        {
            case SalvageExtraction:
                break;
            case SalvageStructure:
                EnsureComp<SalvageStructureExpeditionComponent>(mapUid);
                break;
            default:
                return;
        }

        var landingPadRadius = 16;
        var radiusThickness = 2;
        var dungeonOffset = config.DungeonPosition;
        var dungeonRadius = config.DungeonRadius;
        var dungeonConfig = _prototypeManager.Index<DungeonConfigPrototype>(config.DungeonConfigPrototype);

        var dungeon = _dungeon.GetDungeon(dungeonConfig, dungeonRadius, random);

        // Aborty
        if (dungeon.Rooms.Count == 0)
        {
            return;
        }

        var adjustedDungeonAllTiles = new HashSet<Vector2i>(dungeon.AllTiles.Count);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var tile in room.Tiles)
            {
                adjustedDungeonAllTiles.Add(tile + dungeonOffset);
            }
        }

        // To ensure they can get from the landing area to the dungeon we'll path to the closest tile.
        var closestTile = Vector2i.Zero;
        var closestDistance = float.MaxValue;

        foreach (var tile in adjustedDungeonAllTiles)
        {
            var length = tile.Length;

            if (length < closestDistance)
            {
                closestDistance = length;
                closestTile = tile;
            }
        }

        var start = Vector2i.Zero;
        var reservedTiles = _pathfinding.GetPath(start, closestTile);

        _dungeon.SpawnDungeonTiles(dungeonOffset, dungeon, grid, reservedTiles);

        // Handle loot
        var lootTable = _prototypeManager.Index<WeightedRandomPrototype>(config.Loot);

        _dungeon.SpawnDungeonLoot(dungeonOffset, dungeon, _prototypeManager.Index<LootPrototype>(lootTable.Pick(random)), grid, random, reservedTiles);

        _dungeon.SpawnDungeonWalls(dungeonOffset, dungeon, grid, reservedTiles);

        // Setup the landing pad
        var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
        var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);
        var landingFloor = new HashSet<Vector2i>();

        // Set the tiles themselves
        var seed = new FastNoiseLite(mission.Seed);

        foreach (var tile in grid.GetTilesIntersecting(new Box2(-landingPadRadius - radiusThickness + 0.5f, -landingPadRadius - radiusThickness + 0.5f, landingPadRadius + radiusThickness - 0.5f, landingPadRadius + radiusThickness - 0.5f), false))
        {
            if (!_biome.TryGetBiomeTile(mapUid, grid, seed, tile.GridIndices, out var tileRef))
                continue;

            // TODO: Force load API or smth
            tiles.Add((tile.GridIndices, tileRef.Value));
            landingFloor.Add(tile.GridIndices);
        }

        grid.SetTiles(tiles);

        // Set the outline as enclosed for the landing pad.
        for (var i = 0f; i < radiusThickness; i += 0.5f)
        {
            foreach (var tile in grid.GetTilesOutline(new Circle(Vector2.Zero, landingPadRadius + i), false))
            {
                if (reservedTiles.Contains(tile.GridIndices))
                    continue;

                var anchored = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);

                // Don't overlap for whatever reason.
                if (anchored.MoveNext(out _))
                    continue;

                Spawn("WallSolid", grid.GridTileToLocal(tile.GridIndices));
                landingFloor.Add(tile.GridIndices);
            }
        }

        // Alright now we'll enclose the reserved tiles
        foreach (var tile in reservedTiles)
        {
            // We hit the dungeon so exit out.
            if (adjustedDungeonAllTiles.Contains(tile))
                break;

            for (var i = 0; i < 4; i++)
            {
                var direction = (DirectionFlag) Math.Pow(2, i);
                var neighbor = tile + direction.AsDir().ToIntVec();

                if (reservedTiles.Contains(neighbor) ||
                    adjustedDungeonAllTiles.Contains(neighbor) ||
                    landingFloor.Contains(neighbor))
                {
                    continue;
                }

                if (!_biome.TryGetBiomeTile(mapUid, grid, seed, neighbor, out var tileRef))
                    continue;

                // There shouldn't be many of these so we won't bulk them.
                grid.SetTile(neighbor, tileRef.Value);
                Spawn("WallSolid", grid.GridTileToLocal(neighbor));
            }
        }

        SetupMission(config.Expedition, dungeonOffset, dungeon, grid, random);
    }
}
