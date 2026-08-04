using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Events;
using Content.Server.Tiles;
using Content.Shared.Atmos;
using Content.Shared.Chasm;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.DeadSpace.Lavaland;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.Mining.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Shuttles.Components;
using Content.Shared.Warps;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class LavalandSystem : EntitySystem
{
    private const int BoundaryBatchSize = 256;
    private const int StructureFootprintPadding = 96;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly LavalandBossArenaSystem _bossArena = default!;
    [Dependency] private readonly LavalandFaunaPopulationSystem _faunaPopulation = default!;
    [Dependency] private readonly LavalandNecropolisTendrilPlacementSystem _tendrilPlacement = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private List<Entity<MapGridComponent>> _nearbyGrids = new();
    private readonly List<(Vector2i Index, Tile Tile)> _reservedTiles = new();
    private readonly List<(Vector2i Index, Tile Tile)> _terrainTiles = new();
    private readonly List<EntityUid> _anchoredToDelete = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        SubscribeLocalEvent<StationLavalandComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<StationLavalandComponent, ComponentShutdown>(OnStationShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnStationPostInit(Entity<StationLavalandComponent> ent, ref StationPostInitEvent args)
    {
        if (!_configuration.GetCVar(CCCCVars.LavalandAutoGenerate))
            return;

        if (ent.Comp.GeneratedMap is { Valid: true } generated && Exists(generated))
            return;

        if (ent.Comp.GenerationTask is { IsCompleted: false })
            return;

        if (ent.Comp.Planets.Count == 0)
            return;

        var planetId = _random.Pick(ent.Comp.Planets);
        var planet = _prototype.Index(planetId);

        StartPlanetGeneration(ent, planet, planetId);
    }

    private void StartPlanetGeneration(
        Entity<StationLavalandComponent> station,
        LavalandPlanetPrototype planet,
        ProtoId<LavalandPlanetPrototype> planetId)
    {
        var cancel = new CancellationTokenSource();
        station.Comp.GenerationCancel = cancel;
        station.Comp.GenerationTask = GeneratePlanetDeferred(station.Owner, station.Comp, planet, planetId, cancel);
    }

    private async Task GeneratePlanetDeferred(
        EntityUid station,
        StationLavalandComponent component,
        LavalandPlanetPrototype planet,
        ProtoId<LavalandPlanetPrototype> planetId,
        CancellationTokenSource cancel)
    {
        try
        {
            await GeneratePlanet(station, component, planet, planetId, cancel.Token);
        }
        catch (OperationCanceledException)
        {
            DeleteGeneratedMap(component);
        }
        catch (Exception) when (IsGenerationCancellation(component, cancel.Token))
        {
            DeleteGeneratedMap(component);
        }
        catch (Exception e)
        {
            DeleteGeneratedMap(component);
            Log.Error($"Failed to generate Lavaland planet {planet.ID}: {e}");
        }
        finally
        {
            if (ReferenceEquals(component.GenerationCancel, cancel))
            {
                component.GenerationCancel = null;
                component.GenerationTask = null;
                cancel.Dispose();
            }
        }
    }

    private void OnStationShutdown(Entity<StationLavalandComponent> ent, ref ComponentShutdown args)
    {
        CancelGeneration(ent.Comp);
        DeleteGeneratedMap(ent.Comp);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        var query = EntityQueryEnumerator<StationLavalandComponent>();
        while (query.MoveNext(out _, out var component))
        {
            CancelGeneration(component);
            DeleteGeneratedMap(component);
        }
    }

    private void CancelGeneration(StationLavalandComponent component)
    {
        component.GenerationCancel?.Cancel();
    }

    private void DeleteGeneratedMap(StationLavalandComponent component)
    {
        if (component.GeneratedMap is not { Valid: true } map)
        {
            component.GeneratedMap = null;
            return;
        }

        if (Exists(map) && !Deleted(map))
            QueueDel(map);

        component.GeneratedMap = null;
    }

    private async Task<EntityUid> GeneratePlanet(
        EntityUid station,
        StationLavalandComponent component,
        LavalandPlanetPrototype planet,
        ProtoId<LavalandPlanetPrototype> planetId,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();

        var seed = _random.Next();
        var random = new Random(seed);
        var mapUid = _map.CreateMap(out var mapId, runMapInit: false);
        var grid = EnsureComp<MapGridComponent>(mapUid);
        component.GeneratedMap = mapUid;

        cancellation.ThrowIfCancellationRequested();

        SetupMetadata(mapUid, planet);
        var biome = SetupBiome(mapUid, planet, seed);

        var marker = AddComp<LavalandMapComponent>(mapUid);
        marker.Station = station;
        marker.Planet = planetId;
        marker.Seed = seed;

        _map.InitializeMap(mapId);
        _map.SetPaused(mapUid, true);

        await PrepareMapBoundary(mapUid, grid, planet, random, cancellation);
        PrepareTerminalReservation(mapUid, grid, biome, planet, random);
        var terminalGrid = LoadTerminalGrid(mapId, mapUid, station, planet, planetId);
        PrepareLandingPad(mapUid, grid, biome, planet, random);
        PreloadLandingArea(mapUid, biome, planet);
        cancellation.ThrowIfCancellationRequested();

        if (planet.LandingSite != null)
        {
            var landingSite = _prototype.Index(planet.LandingSite.Value);
            await TryGenerateDungeon(mapUid, grid, landingSite, Vector2i.Zero, random.Next(), cancellation);
        }

        var placedStructures = new List<Vector2i>();
        await GenerateStructures(mapUid, grid, planet, random, placedStructures, cancellation);
        cancellation.ThrowIfCancellationRequested();

        await _tendrilPlacement.SetupMap(mapUid, planet, cancellation);
        LoadCustomGrids(mapId, mapUid, grid, biome, planet, random, placedStructures, cancellation);
        await _bossArena.SpawnConfiguredArenas(mapUid, grid, biome, planet, random, cancellation);
        await _faunaPopulation.SetupMap(mapUid, planet, cancellation);

        cancellation.ThrowIfCancellationRequested();
        CreateLandingWarp(mapUid, planet);
        CreateFtlBeacon(mapUid, planet, terminalGrid);
        SetupFtl(mapUid, planet);
        _map.SetPaused(mapUid, false);
        return mapUid;
    }

    private void CreateLandingWarp(EntityUid mapUid, LavalandPlanetPrototype planet)
    {
        var warpUid = Spawn("GhostWarpPoint", new EntityCoordinates(mapUid, planet.JaunterDestinationOffset));
        var warp = EnsureComp<WarpPointComponent>(warpUid);
        warp.Location = "Лаваленд";
        Dirty(warpUid, warp);
    }

    private void CreateFtlBeacon(EntityUid mapUid, LavalandPlanetPrototype planet, EntityUid? terminalGrid)
    {
        if (!planet.FtlEnabled)
            return;

        var beaconUid = Spawn(null, new EntityCoordinates(mapUid, planet.FtlBeaconOffset));
        _metadata.SetEntityName(beaconUid, planet.FtlBeaconName);
        EnsureComp<FTLBeaconComponent>(beaconUid);

        var dockingBeacon = EnsureComp<FTLDockingBeaconComponent>(beaconUid);
        dockingBeacon.TargetGrid = terminalGrid;
        dockingBeacon.DockWhitelist = planet.FtlDockWhitelist;
        dockingBeacon.FallbackMinOffset = planet.FtlFallbackMinOffset;
        dockingBeacon.FallbackMaxOffset = planet.FtlFallbackMaxOffset;
    }

    private void CreateFtlExclusion(EntityUid mapUid, Vector2 position, float range)
    {
        if (range <= 0f)
            return;

        var exclusionUid = Spawn(null, new EntityCoordinates(mapUid, position));
        _metadata.SetEntityName(exclusionUid, "Lavaland FTL exclusion");

        var exclusion = EnsureComp<LavalandFtlExclusionComponent>(exclusionUid);
        exclusion.Range = range;
    }

    private void CreateDungeonFtlExclusions(EntityUid mapUid, List<Dungeon> dungeons)
    {
        const float extraPadding = 16f;

        foreach (var dungeon in dungeons)
        {
            if (dungeon.AllTiles.Count == 0)
                continue;

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var tile in dungeon.AllTiles)
            {
                minX = Math.Min(minX, tile.X);
                minY = Math.Min(minY, tile.Y);
                maxX = Math.Max(maxX, tile.X);
                maxY = Math.Max(maxY, tile.Y);
            }

            var size = new Vector2(maxX - minX + 1, maxY - minY + 1);
            var center = new Vector2(minX + size.X / 2f, minY + size.Y / 2f);
            var range = size.Length() * 0.5f + extraPadding;
            CreateFtlExclusion(mapUid, center, range);
        }
    }

    private void SetupMetadata(EntityUid mapUid, LavalandPlanetPrototype planet)
    {
        _metadata.SetEntityName(mapUid, planet.MapName);
    }

    private EntityUid? LoadTerminalGrid(
        MapId mapId,
        EntityUid mapUid,
        EntityUid station,
        LavalandPlanetPrototype planet,
        ProtoId<LavalandPlanetPrototype> planetId)
    {
        if (planet.TerminalGridPath == null)
            return null;

        if (!_mapLoader.TryLoadGrid(mapId, planet.TerminalGridPath.Value, out var terminalGrid, offset: planet.TerminalGridOffset))
        {
            Log.Error($"Failed to load Lavaland terminal grid {planet.TerminalGridPath.Value} for planet {planet.ID}.");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(planet.TerminalGridName))
            _metadata.SetEntityName(terminalGrid.Value, planet.TerminalGridName);

        var marker = EnsureComp<LavalandOutpostComponent>(terminalGrid.Value);
        marker.Station = station;
        marker.Map = mapUid;
        marker.Planet = planetId;

        return terminalGrid.Value;
    }

    private void SetupFtl(EntityUid mapUid, LavalandPlanetPrototype planet)
    {
        if (!planet.FtlEnabled)
            return;

        var destination = EnsureComp<FTLDestinationComponent>(mapUid);
        destination.Enabled = true;
        destination.BeaconsOnly = planet.FtlBeaconsOnly;
        destination.RequireCoordinateDisk = planet.RequireCoordinateDisk;
        destination.Whitelist = planet.FtlWhitelist;
        Dirty(mapUid, destination);
    }

    private BiomeComponent SetupBiome(EntityUid mapUid, LavalandPlanetPrototype planet, int seed)
    {
        var biome = EntityManager.ComponentFactory.GetComponent<BiomeComponent>();
        _biome.SetSeed(mapUid, biome, seed, false);
        _biome.SetTemplate(mapUid, biome, _prototype.Index(planet.Biome), false);
        _biome.SetBounds(mapUid, biome, CreateMapBounds(planet), false);
        AddComp(mapUid, biome, true);

        foreach (var markerLayer in planet.MarkerLayers)
        {
            _biome.AddMarkerLayer(mapUid, biome, markerLayer);
        }

        if (planet.Gravity)
        {
            var gravity = EnsureComp<GravityComponent>(mapUid);
            gravity.Enabled = true;
            gravity.Inherent = true;
            Dirty(mapUid, gravity);
        }

        if (planet.LightColor != null)
        {
            var light = EnsureComp<MapLightComponent>(mapUid);
            light.AmbientLightColor = planet.LightColor.Value;
            Dirty(mapUid, light);
        }

        var atmosphere = planet.Atmosphere != null
            ? CopyAtmosphere(planet.Atmosphere)
            : CreateDefaultAtmosphere();
        _atmosphere.SetMapAtmosphere(mapUid, false, atmosphere);

        return biome;
    }

    private static Box2i? CreateMapBounds(LavalandPlanetPrototype planet)
    {
        if (planet.MapHalfSize <= 0)
            return null;

        var halfSize = Math.Max(1, planet.MapHalfSize);
        return new Box2i(-halfSize, -halfSize, halfSize, halfSize);
    }

    private static GasMixture CreateDefaultAtmosphere()
    {
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 14f;
        moles[(int) Gas.Nitrogen] = 23f;
        return new GasMixture(moles, 300f);
    }

    private static GasMixture CopyAtmosphere(GasMixture atmosphere)
    {
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        foreach (var (gas, amount) in atmosphere)
        {
            moles[(int) gas] = amount;
        }

        return new GasMixture(moles, atmosphere.Temperature, atmosphere.Volume);
    }

    private void PrepareLandingPad(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        LavalandPlanetPrototype planet,
        Random random)
    {
        var radius = Math.Max(1, planet.LandingPadRadius);
        var bounds = Box2.CenteredAround(Vector2.Zero, new Vector2(radius * 2 + 1, radius * 2 + 1));
        var reserved = new List<(Vector2i Index, Tile Tile)>();
        _biome.ReserveTiles(mapUid, bounds, reserved, biome, grid);

        var tileDef = _tileDefinition[planet.LandingPadTile];

        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var radiusSquared = radius * radius;

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radiusSquared)
                    continue;

                tiles.Add((new Vector2i(x, y), CreateTile(tileDef, random)));
            }
        }

        _map.SetTiles(mapUid, grid, tiles);
    }

    private async Task PrepareMapBoundary(
        EntityUid mapUid,
        MapGridComponent grid,
        LavalandPlanetPrototype planet,
        Random random,
        CancellationToken cancellation)
    {
        if (!planet.BoundaryEnabled || planet.MapHalfSize <= 0)
            return;

        var halfSize = Math.Max(1, planet.MapHalfSize);
        var lavaWidth = Math.Max(0, planet.BoundaryLavaWidth);
        var wallWidth = Math.Max(1, planet.BoundaryWallWidth);
        var boundaryWidth = Math.Min(halfSize, lavaWidth + wallWidth);
        var effectiveLavaWidth = Math.Min(lavaWidth, boundaryWidth);

        if (boundaryWidth <= 0)
            return;

        var tileDef = _tileDefinition[planet.BoundaryTile];
        var tiles = new List<(Vector2i Index, Tile Tile)>(BoundaryBatchSize);
        var lavaTiles = new List<Vector2i>(BoundaryBatchSize);
        var wallTiles = new List<Vector2i>(BoundaryBatchSize);

        foreach (var index in EnumerateSquareRing(halfSize, boundaryWidth))
        {
            cancellation.ThrowIfCancellationRequested();

            tiles.Add((index, CreateTile(tileDef, random)));
            if (GetDistanceToSquareEdge(index.X, index.Y, halfSize) < effectiveLavaWidth)
                lavaTiles.Add(index);
            else
                wallTiles.Add(index);

            if (tiles.Count < BoundaryBatchSize)
                continue;

            FlushBoundaryBatch(mapUid, grid, planet, tiles, lavaTiles, wallTiles);
            await Timer.Delay(1, cancellation);
        }

        if (tiles.Count > 0)
            FlushBoundaryBatch(mapUid, grid, planet, tiles, lavaTiles, wallTiles);
    }

    private void FlushBoundaryBatch(
        EntityUid mapUid,
        MapGridComponent grid,
        LavalandPlanetPrototype planet,
        List<(Vector2i Index, Tile Tile)> tiles,
        List<Vector2i> lavaTiles,
        List<Vector2i> wallTiles)
    {
        _map.SetTiles(mapUid, grid, tiles);

        foreach (var tile in lavaTiles)
            SpawnAnchored(planet.BoundaryLavaEntity, mapUid, grid, tile);

        foreach (var tile in wallTiles)
            SpawnAnchored(planet.BoundaryWallEntity, mapUid, grid, tile);

        tiles.Clear();
        lavaTiles.Clear();
        wallTiles.Clear();
    }

    private static IEnumerable<Vector2i> EnumerateSquareRing(int halfSize, int ringWidth)
    {
        var min = -halfSize;
        var max = halfSize;
        var innerMin = min + ringWidth;
        var innerMax = max - ringWidth;

        for (var x = min; x < max; x++)
        {
            for (var y = min; y < innerMin; y++)
                yield return new Vector2i(x, y);

            for (var y = innerMax; y < max; y++)
            {
                if (y >= innerMin)
                    yield return new Vector2i(x, y);
            }
        }

        for (var y = innerMin; y < innerMax; y++)
        {
            for (var x = min; x < innerMin; x++)
                yield return new Vector2i(x, y);

            for (var x = innerMax; x < max; x++)
                yield return new Vector2i(x, y);
        }
    }

    private void SpawnAnchored(
        string prototype,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i index)
    {
        var uid = Spawn(prototype, _map.GridTileToLocal(gridUid, grid, index));

        if (!_xformQuery.TryGetComponent(uid, out var xform) || xform.Anchored)
            return;

        _transform.AnchorEntity((uid, xform), (gridUid, grid), index);
    }

    private static int GetDistanceToSquareEdge(int x, int y, int halfSize)
    {
        var left = x + halfSize;
        var right = halfSize - 1 - x;
        var bottom = y + halfSize;
        var top = halfSize - 1 - y;

        return Math.Min(Math.Min(left, right), Math.Min(bottom, top));
    }

    private void PrepareTerminalReservation(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        LavalandPlanetPrototype planet,
        Random random)
    {
        if (!TryGetTerminalReservationBounds(planet, out var bounds))
            return;

        var reserved = new List<(Vector2i Index, Tile Tile)>();
        _biome.ReserveTiles(mapUid, ToBox2(bounds), reserved, biome, grid);

        var tileDef = _tileDefinition[planet.TerminalTile];
        var tiles = new List<(Vector2i Index, Tile Tile)>();

        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            for (var y = bounds.Bottom; y < bounds.Top; y++)
            {
                tiles.Add((new Vector2i(x, y), CreateTile(tileDef, random)));
            }
        }

        _map.SetTiles(mapUid, grid, tiles);
    }

    private Tile CreateTile(ITileDefinition tileDef, Random random)
    {
        return new Tile(tileDef.TileId,
            variant: tileDef is ContentTileDefinition contentTile
                ? _tile.PickVariant(contentTile, random)
                : (byte) 0);
    }

    private void PreloadLandingArea(EntityUid mapUid, BiomeComponent biome, LavalandPlanetPrototype planet)
    {
        var radius = planet.LandingPadRadius + 12;
        var preload = Box2.CenteredAround(Vector2.Zero, new Vector2(radius * 2, radius * 2));
        _biome.Preload(mapUid, biome, preload);

        if (TryGetTerminalReservationBounds(planet, out var terminalBounds))
            _biome.Preload(mapUid, biome, ToBox2(terminalBounds).Enlarged(12f));

        if (planet.FtlEnabled)
        {
            var ftlPreload = Box2.CenteredAround(planet.FtlBeaconOffset, new Vector2(radius * 2, radius * 2));
            _biome.Preload(mapUid, biome, ftlPreload);
        }
    }

    private async Task GenerateStructures(
        EntityUid mapUid,
        MapGridComponent grid,
        LavalandPlanetPrototype planet,
        Random random,
        List<Vector2i> placed,
        CancellationToken cancellation)
    {
        if (planet.Structures.Count == 0)
            return;

        var structures = new List<DungeonConfigPrototype>();
        foreach (var (dungeon, count) in planet.Structures)
        {
            var proto = _prototype.Index(dungeon);
            for (var i = 0; i < count; i++)
            {
                structures.Add(proto);
            }
        }

        _random.Shuffle(structures);

        foreach (var structure in structures)
        {
            cancellation.ThrowIfCancellationRequested();
            var position = PickStructurePosition(planet, random, placed);
            placed.Add(position);
            await TryGenerateDungeon(mapUid, grid, structure, position, random.Next(), cancellation);
        }
    }

    private void LoadCustomGrids(
        MapId mapId,
        EntityUid mapUid,
        MapGridComponent terrainGrid,
        BiomeComponent biome,
        LavalandPlanetPrototype planet,
        Random random,
        List<Vector2i> placed,
        CancellationToken cancellation)
    {
        if (planet.CustomGrids.Count == 0)
            return;

        var grids = new List<LavalandCustomGridEntry>();
        foreach (var entry in planet.CustomGrids)
        {
            var count = Math.Max(0, entry.Count);
            for (var i = 0; i < count; i++)
            {
                grids.Add(entry);
            }
        }

        if (grids.Count == 0)
            return;

        _random.Shuffle(grids);

        var loaded = 0;
        foreach (var entry in grids)
        {
            cancellation.ThrowIfCancellationRequested();

            if (!TryPickCustomGridPosition(mapUid, terrainGrid, planet, entry, random, placed, out var position))
            {
                Log.Warning($"Failed to place Lavaland custom grid {entry.Path} for planet {planet.ID}.");
                continue;
            }

            var footprint = Math.Max(1, entry.FootprintRadius);
            var footprintBounds = GetCustomGridFootprintBounds(position, footprint);
            PrepareCustomGridTerrain(mapUid, terrainGrid, biome, planet, random, footprintBounds);

            if (!_mapLoader.TryLoadGrid(mapId, entry.Path, out var customGrid))
            {
                Log.Error($"Failed to load Lavaland custom grid {entry.Path} for planet {planet.ID} at {position}.");
                continue;
            }

            if (customGrid == null || TerminatingOrDeleted(customGrid.Value.Owner))
            {
                Log.Error($"Lavaland custom grid loader returned no grid for {entry.Path} on planet {planet.ID} at {position}.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.Name))
                _metadata.SetEntityName(customGrid.Value, entry.Name);

            _transform.SetMapCoordinates(customGrid.Value.Owner, new MapCoordinates(new Vector2(position.X, position.Y), mapId));
            var exclusion = EnsureComp<LavalandFtlExclusionComponent>(customGrid.Value.Owner);
            var gridRadius = customGrid.Value.Comp.LocalAABB.Size.Length() * 0.5f;
            exclusion.Range = Math.Max(Math.Max(1, entry.FootprintRadius), gridRadius) + 16f;
            placed.Add(position);
            loaded++;
        }

        Log.Info($"Lavaland custom grids loaded {loaded}/{grids.Count} for planet {planet.ID}.");
    }

    private bool TryPickCustomGridPosition(
        EntityUid mapUid,
        MapGridComponent terrainGrid,
        LavalandPlanetPrototype planet,
        LavalandCustomGridEntry entry,
        Random random,
        List<Vector2i> placed,
        out Vector2i position)
    {
        position = default;

        var footprint = Math.Max(1, entry.FootprintRadius);
        var mapLimit = GetCustomGridPlacementLimit(planet, footprint);
        var minSeparation = Math.Max(1, entry.MinSeparation ?? Math.Max(planet.MinStructureSeparation, footprint * 2));
        var exclusionPadding = Math.Max(footprint, minSeparation);
        var reservedDistance = GetTerminalReservedDistance(planet) + exclusionPadding;
        var requestedMinDistance = Math.Max(
            entry.MinDistance ?? planet.MinStructureDistance,
            Math.Max(planet.LandingPadRadius + exclusionPadding, reservedDistance));
        var minDistance = Math.Min(requestedMinDistance, mapLimit);
        var maxDistance = Math.Clamp(entry.MaxDistance ?? planet.MaxStructureDistance, minDistance, mapLimit);
        var minSeparationSquared = minSeparation * minSeparation;
        var attempts = Math.Max(1, entry.SpawnAttempts);

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var angle = random.NextDouble() * MathF.PI * 2f;
            var distance = random.Next(minDistance, maxDistance + 1);
            var candidate = new Vector2i(
                (int) MathF.Round(MathF.Cos((float) angle) * distance),
                (int) MathF.Round(MathF.Sin((float) angle) * distance));

            if (IsInsideStructureExclusion(candidate, planet, exclusionPadding) ||
                !IsSeparatedFromPlaced(candidate, placed, minSeparationSquared) ||
                HasNearbyNonTerrainGrid(mapUid, new Vector2(candidate.X, candidate.Y), footprint + minSeparation) ||
                HasPersistentAnchoredEntityInFootprint(mapUid, terrainGrid, candidate, footprint))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private void PrepareCustomGridTerrain(
        EntityUid mapUid,
        MapGridComponent terrainGrid,
        BiomeComponent biome,
        LavalandPlanetPrototype planet,
        Random random,
        Box2i bounds)
    {
        _reservedTiles.Clear();
        _biome.ReserveTiles(mapUid, ToBox2(bounds), _reservedTiles, biome, terrainGrid);
        _reservedTiles.Clear();

        ClearClearableTerrainEntities(mapUid, terrainGrid, bounds);

        var tileDef = _tileDefinition[planet.BoundaryTile];
        _terrainTiles.Clear();
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            for (var y = bounds.Bottom; y < bounds.Top; y++)
            {
                _terrainTiles.Add((new Vector2i(x, y), CreateTile(tileDef, random)));
            }
        }

        _map.SetTiles(mapUid, terrainGrid, _terrainTiles);
        _terrainTiles.Clear();
    }

    private Vector2i PickStructurePosition(
        LavalandPlanetPrototype planet,
        Random random,
        List<Vector2i> placed)
    {
        var mapLimit = GetStructurePlacementLimit(planet);
        var reservedDistance = GetTerminalReservedDistance(planet) + planet.MinStructureSeparation;
        var requestedMinDistance = Math.Max(
            planet.MinStructureDistance,
            Math.Max(planet.LandingPadRadius + 8, reservedDistance));
        var minDistance = Math.Min(requestedMinDistance, mapLimit);
        var maxDistance = Math.Clamp(planet.MaxStructureDistance, minDistance, mapLimit);
        var minSeparationSquared = planet.MinStructureSeparation * planet.MinStructureSeparation;

        for (var attempt = 0; attempt < 96; attempt++)
        {
            var angle = random.NextDouble() * MathF.PI * 2f;
            var distance = random.Next(minDistance, maxDistance + 1);
            var candidate = new Vector2i(
                (int) MathF.Round(MathF.Cos((float) angle) * distance),
                (int) MathF.Round(MathF.Sin((float) angle) * distance));

            if (IsInsideStructureExclusion(candidate, planet, planet.MinStructureSeparation))
                continue;

            if (IsSeparatedFromPlaced(candidate, placed, minSeparationSquared))
                return candidate;
        }

        for (var attempt = 0; attempt < 64; attempt++)
        {
            var fallbackAngle = random.NextDouble() * MathF.PI * 2f;
            var fallback = new Vector2i(
                (int) MathF.Round(MathF.Cos((float) fallbackAngle) * maxDistance),
                (int) MathF.Round(MathF.Sin((float) fallbackAngle) * maxDistance));

            if (!IsInsideStructureExclusion(fallback, planet, planet.MinStructureSeparation) &&
                IsSeparatedFromPlaced(fallback, placed, minSeparationSquared))
            {
                return fallback;
            }
        }

        return new Vector2i(maxDistance, 0);
    }

    private static bool IsSeparatedFromPlaced(Vector2i candidate, List<Vector2i> placed, int minSeparationSquared)
    {
        foreach (var other in placed)
        {
            if ((candidate - other).LengthSquared < minSeparationSquared)
                return false;
        }

        return true;
    }

    private static bool IsInsideStructureExclusion(
        Vector2i position,
        LavalandPlanetPrototype planet,
        int padding)
    {
        var pos = new Vector2(position.X, position.Y);

        var landingRadius = planet.LandingPadRadius + padding;
        if (pos.LengthSquared() <= landingRadius * landingRadius)
            return true;

        if (IsInsideTerminalReservation(position, planet, padding))
            return true;

        if (planet.FtlEnabled)
        {
            var ftlRadius = Math.Max(planet.FaunaFtlBeaconExclusionRadius, 32f) + padding;
            if (Vector2.DistanceSquared(pos, planet.FtlBeaconOffset) <= ftlRadius * ftlRadius)
                return true;
        }

        return false;
    }

    private static int GetStructurePlacementLimit(LavalandPlanetPrototype planet)
    {
        if (planet.MapHalfSize <= 0)
            return Math.Max(1, planet.MaxStructureDistance);

        var edgePadding = 16;
        if (planet.BoundaryEnabled)
            edgePadding += Math.Max(0, planet.BoundaryLavaWidth) + Math.Max(1, planet.BoundaryWallWidth);

        edgePadding += StructureFootprintPadding;

        return Math.Max(1, planet.MapHalfSize - edgePadding);
    }

    private static int GetCustomGridPlacementLimit(LavalandPlanetPrototype planet, int footprint)
    {
        if (planet.MapHalfSize <= 0)
            return Math.Max(1, planet.MaxStructureDistance);

        var edgePadding = 16 + Math.Max(1, footprint);
        if (planet.BoundaryEnabled)
            edgePadding += Math.Max(0, planet.BoundaryLavaWidth) + Math.Max(1, planet.BoundaryWallWidth);

        return Math.Max(1, planet.MapHalfSize - edgePadding);
    }

    private static Box2i GetCustomGridFootprintBounds(Vector2i center, int radius)
    {
        radius = Math.Max(1, radius);
        return new Box2i(
            center.X - radius,
            center.Y - radius,
            center.X + radius + 1,
            center.Y + radius + 1);
    }

    private static bool IsInsideTerminalReservation(
        Vector2i position,
        LavalandPlanetPrototype planet,
        int padding)
    {
        if (!TryGetTerminalReservationBounds(planet, out var bounds))
            return false;

        return position.X >= bounds.Left - padding &&
               position.X < bounds.Right + padding &&
               position.Y >= bounds.Bottom - padding &&
               position.Y < bounds.Top + padding;
    }

    private static int GetTerminalReservedDistance(LavalandPlanetPrototype planet)
    {
        return planet.TerminalReservationEnabled
            ? (Math.Max(1, planet.TerminalReservationSize) + 1) / 2
            : 0;
    }

    private static bool TryGetTerminalReservationBounds(
        LavalandPlanetPrototype planet,
        out Box2i bounds)
    {
        bounds = default;

        if (!planet.TerminalReservationEnabled)
            return false;

        var size = Math.Max(1, planet.TerminalReservationSize);
        var min = -size / 2;
        bounds = new Box2i(min, min, min + size, min + size);
        return true;
    }

    private static Box2 ToBox2(Box2i bounds)
    {
        return new Box2(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top);
    }

    private bool HasNearbyNonTerrainGrid(EntityUid terrainGridUid, Vector2 center, float radius)
    {
        _nearbyGrids.Clear();

        var mapId = Transform(terrainGridUid).MapID;
        var bounds = Box2.CenteredAround(center, Vector2.One * (radius * 2f + 1f));
        _mapManager.FindGridsIntersecting(mapId, bounds, ref _nearbyGrids);

        foreach (var grid in _nearbyGrids)
        {
            if (grid.Owner != terrainGridUid)
                return true;
        }

        return false;
    }

    private bool HasPersistentAnchoredEntityInFootprint(
        EntityUid terrainGridUid,
        MapGridComponent terrainGrid,
        Vector2i center,
        int radius)
    {
        var bounds = new Box2i(
            center.X - radius,
            center.Y - radius,
            center.X + radius + 1,
            center.Y + radius + 1);

        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            for (var y = bounds.Bottom; y < bounds.Top; y++)
            {
                var anchored = _map.GetAnchoredEntitiesEnumerator(terrainGridUid, terrainGrid, new Vector2i(x, y));
                while (anchored.MoveNext(out var uid))
                {
                    if (uid == null ||
                        TerminatingOrDeleted(uid.Value) ||
                        IsClearableTerrainEntity(uid.Value))
                    {
                        continue;
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private void ClearClearableTerrainEntities(
        EntityUid terrainGridUid,
        MapGridComponent terrainGrid,
        Box2i bounds)
    {
        _anchoredToDelete.Clear();

        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            for (var y = bounds.Bottom; y < bounds.Top; y++)
            {
                var anchored = _map.GetAnchoredEntitiesEnumerator(terrainGridUid, terrainGrid, new Vector2i(x, y));
                while (anchored.MoveNext(out var uid))
                {
                    if (uid != null &&
                        !TerminatingOrDeleted(uid.Value) &&
                        IsClearableTerrainEntity(uid.Value))
                    {
                        _anchoredToDelete.Add(uid.Value);
                    }
                }
            }
        }

        foreach (var uid in _anchoredToDelete)
        {
            if (!TerminatingOrDeleted(uid))
                QueueDel(uid);
        }

        _anchoredToDelete.Clear();
    }

    private bool IsClearableTerrainEntity(EntityUid uid)
    {
        if (HasComp<TileEntityEffectComponent>(uid) ||
            HasComp<ChasmComponent>(uid) ||
            HasComp<OreVeinComponent>(uid))
        {
            return true;
        }

        var prototypeId = MetaData(uid).EntityPrototype?.ID;
        return prototypeId is not null &&
               (prototypeId.StartsWith("WallRockBasalt", StringComparison.Ordinal) ||
                prototypeId is "FloorLavaEntity" or "FloorChasmEntity");
    }

    private async Task GenerateDungeon(
        EntityUid mapUid,
        MapGridComponent grid,
        DungeonConfigPrototype dungeon,
        Vector2i position,
        int seed,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var result = await _dungeon.GenerateDungeonAsync(dungeon, mapUid, grid, position, seed, cancellation);
        cancellation.ThrowIfCancellationRequested();

        if (result.Count == 0 || result[0].Rooms.Count == 0)
        {
            Log.Warning($"Lavaland dungeon {dungeon.ID} generated no rooms at {position}.");
            return;
        }

        CreateDungeonFtlExclusions(mapUid, result);

        Log.Info($"Lavaland dungeon {dungeon.ID} generated {result[0].Rooms.Count} rooms at {position}.");
    }

    private async Task TryGenerateDungeon(
        EntityUid mapUid,
        MapGridComponent grid,
        DungeonConfigPrototype dungeon,
        Vector2i position,
        int seed,
        CancellationToken cancellation)
    {
        try
        {
            await GenerateDungeon(mapUid, grid, dungeon, position, seed, cancellation);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (IsGenerationCancellation(mapUid, cancellation))
        {
            throw new OperationCanceledException("Lavaland dungeon generation was canceled.", e, cancellation);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to generate Lavaland dungeon {dungeon.ID} at {position}: {e}");
        }
    }

    private bool IsGenerationCancellation(StationLavalandComponent component, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return true;

        return component.GeneratedMap is { Valid: true } map && IsGenerationCancellation(map, cancellation);
    }

    private bool IsGenerationCancellation(EntityUid mapUid, CancellationToken cancellation)
    {
        return cancellation.IsCancellationRequested || !Exists(mapUid) || Deleted(mapUid);
    }
}
