using System.Linq;
using System.Numerics;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Extraction;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Random;
using Robust.Shared.Utility;
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
            if (comp.NextOffer < currentTime)
            {
                comp.NextOffer += comp.Cooldown;
                GenerateMissions(comp);
                UpdateConsoles(comp);
            }
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
        component.NextOffer = _timing.CurTime;
        component.MissionCompleted = false;
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
            var mission = new SalvageMission()
            {
                Index = component.NextIndex,
                Config = _random.Pick(configs).ID,
                Seed = _random.Next(),
                Duration = TimeSpan.FromSeconds(_random.Next(9 * 60 / timeBlock, 12 * 60 / timeBlock) * timeBlock),
            };

            component.Missions[component.NextIndex++] = mission;
        }
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.Claimed, component.ActiveMission, missions);
    }

    private void SpawnMission(SalvageMission mission, EntityUid station)
    {
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
        var random = new Random(mission.Seed);

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
        var dungeonOffset = new Vector2i(landingPadRadius + radiusThickness + 1, 0);

        var dungeon = _dungeon.GetDungeon(config.Dungeon);

        // Aborty
        if (dungeon.Rooms.Count == 0)
        {
            return;
        }

        var adjustedDungeonAllTiles = new HashSet<Vector2i>(dungeon.AllTiles.Count);

        foreach (var tile in dungeon.AllTiles)
        {
            adjustedDungeonAllTiles.Add(tile + dungeonOffset);
        }

        // To ensure they can get from the landing area to the dungeon we'll mark out reserved tiles
        // and ensure no blockers spawn on them.
        // We'll pick the bottom-left room as our target.
        var closestDistance = float.MaxValue;
        DungeonRoom? closestRoom = null;

        foreach (var room in dungeon.Rooms)
        {
            var length = room.Center.Length;

            if (length > closestDistance)
                continue;

            closestDistance = length;
            closestRoom = room;
        }

        if (closestRoom == null)
        {
            return;
        }

        var start = Vector2i.Zero;
        var end = closestRoom.Tiles.ElementAt(_random.Next(closestRoom.Tiles.Count)) + dungeonOffset;
        var reservedTiles = _pathfinding.GetPath(start, end);

        // TODO: Spawn boundaries
        _dungeon.SpawnDungeon(dungeonOffset, dungeon, _prototypeManager.Index<DungeonConfigPrototype>(config.DungeonConfigPrototype), grid, reservedTiles);

        // Setup the landing pad
        var landingPadExtents = new Vector2i(landingPadRadius, landingPadRadius);
        var tiles = new List<(Vector2i Indices, Tile Tile)>(landingPadExtents.X * landingPadExtents.Y * 2);
        var landingFloor = new HashSet<Vector2i>();

        // Set the tiles themselves
        var seed = new FastNoise(mission.Seed);

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

        SetupMission(config.Expedition, dungeon, grid, random);
    }
}
