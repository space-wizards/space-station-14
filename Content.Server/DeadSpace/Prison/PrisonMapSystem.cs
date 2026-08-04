using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.Parallax;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.DeadSpace.Prison;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Warps;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Prison;

public sealed class PrisonMapSystem : EntitySystem
{
    private const string PrisonPlanet = "PrisonQuarry";
    private const string PrisonWarpLocation = "Prison Quarry";

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly PrisonFaunaPopulationSystem _faunaPopulation = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityUid? _generatedMap;
    private bool _enabled;
    private bool _generationFailed;

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();

        Subs.CVar(_configuration, CCCCVars.PrisonEnabled, OnPrisonEnabledChanged, true);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || _generationFailed)
            return;

        if (_generatedMap is { Valid: true } generatedMap && Exists(generatedMap) && !Deleted(generatedMap))
            return;

        _generatedMap = null;

        if (HasPrisonSpawnPoint())
            return;

        GeneratePrisonMap();
    }

    private void OnPrisonEnabledChanged(bool enabled)
    {
        _enabled = enabled;

        if (enabled)
        {
            _generationFailed = false;
            return;
        }

        DeleteGeneratedMap();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        DeleteGeneratedMap();
        _generationFailed = false;
    }

    private bool HasPrisonSpawnPoint()
    {
        var query = EntityQueryEnumerator<PrisonSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID != MapId.Nullspace)
                return true;
        }

        return false;
    }

    private void GeneratePrisonMap()
    {
        if (!_prototype.TryIndex<PrisonPlanetPrototype>(PrisonPlanet, out var planet))
        {
            Log.Error($"Unable to generate prison map: prisonPlanet prototype {PrisonPlanet} was not found.");
            _generationFailed = true;
            return;
        }

        try
        {
            var seed = _random.Next();
            var random = new Random(seed);
            var mapUid = _map.CreateMap(out var mapId, runMapInit: false);
            var grid = EnsureComp<MapGridComponent>(mapUid);
            _generatedMap = mapUid;

            SetupMetadata(mapUid, planet);
            SetupFtl(mapUid, planet);
            var biome = SetupBiome(mapUid, planet, seed);
            var marker = AddComp<PrisonMapComponent>(mapUid);
            marker.Planet = PrisonPlanet;

            _map.InitializeMap(mapId);
            _map.SetPaused(mapUid, true);

            PrepareMapBoundary(mapUid, grid, planet, random);
            PrepareResidenceReservation(mapUid, grid, biome, planet, random);
            var residenceGrid = LoadResidenceGrid(mapId, planet);
            PrepareLandingPad(mapUid, grid, biome, planet, random);
            CreateFtlBeacon(mapUid, planet, residenceGrid);
            CreateGhostWarp(mapUid);
            PreloadResidenceArea(mapUid, biome, planet);
            PreloadLandingArea(mapUid, biome, planet);
            _faunaPopulation.SetupMap(mapUid, planet);

            _map.SetPaused(mapUid, false);
            Log.Info($"Generated prison map {planet.ID} with seed {seed}.");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to generate prison map {planet.ID}: {e}");
            DeleteGeneratedMap();
            _generationFailed = true;
        }
    }

    private void DeleteGeneratedMap()
    {
        if (_generatedMap is not { Valid: true } map)
        {
            _generatedMap = null;
            return;
        }

        if (Exists(map) && !Deleted(map))
            QueueDel(map);

        _generatedMap = null;
    }

    private void SetupMetadata(EntityUid mapUid, PrisonPlanetPrototype planet)
    {
        _metadata.SetEntityName(mapUid, planet.MapName);
    }

    private void SetupFtl(EntityUid mapUid, PrisonPlanetPrototype planet)
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

    private BiomeComponent SetupBiome(EntityUid mapUid, PrisonPlanetPrototype planet, int seed)
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

    private static Box2i? CreateMapBounds(PrisonPlanetPrototype planet)
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

    private void PrepareMapBoundary(
        EntityUid mapUid,
        MapGridComponent grid,
        PrisonPlanetPrototype planet,
        Random random)
    {
        if (!planet.BoundaryEnabled || planet.MapHalfSize <= 0)
            return;

        var halfSize = Math.Max(1, planet.MapHalfSize);
        var wallWidth = Math.Max(1, planet.BoundaryWallWidth);
        var boundaryWidth = Math.Min(halfSize, wallWidth);

        if (boundaryWidth <= 0)
            return;

        var tileDef = _tileDefinition[planet.BoundaryTile];
        var capacity = GetSquareRingTileCount(halfSize, boundaryWidth);
        var tiles = new List<(Vector2i Index, Tile Tile)>(capacity);
        var wallTiles = new List<Vector2i>(capacity);

        for (var x = -halfSize; x < halfSize; x++)
        {
            for (var y = -halfSize; y < halfSize; y++)
            {
                var edgeDistance = GetDistanceToSquareEdge(x, y, halfSize);
                if (edgeDistance >= boundaryWidth)
                    continue;

                var index = new Vector2i(x, y);
                tiles.Add((index, CreateTile(tileDef, random)));

                wallTiles.Add(index);
            }
        }

        _map.SetTiles(mapUid, grid, tiles);

        foreach (var tile in wallTiles)
        {
            SpawnAnchored(planet.BoundaryWallEntity, mapUid, grid, tile);
        }
    }

    private void PrepareResidenceReservation(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        PrisonPlanetPrototype planet,
        Random random)
    {
        if (!TryGetResidenceReservationBounds(planet, out var bounds))
            return;

        var reserved = new List<(Vector2i Index, Tile Tile)>();
        _biome.ReserveTiles(mapUid, ToBox2(bounds), reserved, biome, grid);

        var tileDef = _tileDefinition[planet.ResidenceTile];
        var tiles = new List<(Vector2i Index, Tile Tile)>(bounds.Area);

        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            for (var y = bounds.Bottom; y < bounds.Top; y++)
            {
                tiles.Add((new Vector2i(x, y), CreateTile(tileDef, random)));
            }
        }

        _map.SetTiles(mapUid, grid, tiles);
    }

    private EntityUid? LoadResidenceGrid(MapId mapId, PrisonPlanetPrototype planet)
    {
        if (planet.ResidenceGridPath == null)
            return null;

        if (!_mapLoader.TryLoadGrid(mapId, planet.ResidenceGridPath.Value, out var grid, offset: planet.ResidenceGridOffset))
        {
            Log.Error($"Failed to load prison residence grid {planet.ResidenceGridPath.Value} for planet {planet.ID}.");
            return null;
        }

        if (grid != null && !string.IsNullOrWhiteSpace(planet.ResidenceGridName))
            _metadata.SetEntityName(grid.Value, planet.ResidenceGridName);

        return grid;
    }

    private void PrepareLandingPad(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        PrisonPlanetPrototype planet,
        Random random)
    {
        var radius = Math.Max(1, planet.LandingPadRadius);
        var bounds = Box2.CenteredAround(planet.FtlBeaconOffset, new Vector2(radius * 2 + 1, radius * 2 + 1));
        var reserved = new List<(Vector2i Index, Tile Tile)>();
        _biome.ReserveTiles(mapUid, bounds, reserved, biome, grid);

        var tileDef = _tileDefinition[planet.LandingPadTile];
        var tiles = new List<(Vector2i Index, Tile Tile)>();
        var radiusSquared = radius * radius;
        var center = new Vector2i(
            (int) MathF.Floor(planet.FtlBeaconOffset.X),
            (int) MathF.Floor(planet.FtlBeaconOffset.Y));

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radiusSquared)
                    continue;

                tiles.Add((center + new Vector2i(x, y), CreateTile(tileDef, random)));
            }
        }

        _map.SetTiles(mapUid, grid, tiles);
    }

    private void CreateFtlBeacon(EntityUid mapUid, PrisonPlanetPrototype planet, EntityUid? residenceGrid)
    {
        if (!planet.FtlEnabled)
            return;

        var beaconUid = Spawn(null, new EntityCoordinates(mapUid, planet.FtlBeaconOffset));
        _metadata.SetEntityName(beaconUid, planet.FtlBeaconName);
        EnsureComp<FTLBeaconComponent>(beaconUid);

        var dockingBeacon = EnsureComp<FTLDockingBeaconComponent>(beaconUid);
        dockingBeacon.TargetGrid = residenceGrid;
        dockingBeacon.DockWhitelist = planet.FtlDockWhitelist;
        dockingBeacon.FallbackMinOffset = planet.FtlFallbackMinOffset;
        dockingBeacon.FallbackMaxOffset = planet.FtlFallbackMaxOffset;
    }

    private void CreateGhostWarp(EntityUid mapUid)
    {
        var warpUid = Spawn("GhostWarpPoint", new EntityCoordinates(mapUid, Vector2.Zero));
        var warp = EnsureComp<WarpPointComponent>(warpUid);
        warp.Location = PrisonWarpLocation;
        Dirty(warpUid, warp);

        _transform.AttachToGridOrMap(warpUid);
    }

    private void PreloadResidenceArea(EntityUid mapUid, BiomeComponent biome, PrisonPlanetPrototype planet)
    {
        if (!TryGetResidenceReservationBounds(planet, out var bounds))
            return;

        _biome.Preload(mapUid, biome, ToBox2(bounds).Enlarged(16f));
    }

    private void PreloadLandingArea(EntityUid mapUid, BiomeComponent biome, PrisonPlanetPrototype planet)
    {
        var radius = Math.Max(1, planet.LandingPadRadius);
        var size = new Vector2(radius * 2 + 1, radius * 2 + 1);
        _biome.Preload(mapUid, biome, Box2.CenteredAround(planet.FtlBeaconOffset, size).Enlarged(16f));
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

    private Tile CreateTile(ITileDefinition tileDef, Random random)
    {
        return new Tile(tileDef.TileId,
            variant: tileDef is ContentTileDefinition contentTile
                ? _tile.PickVariant(contentTile, random)
                : (byte) 0);
    }

    private static bool TryGetResidenceReservationBounds(
        PrisonPlanetPrototype planet,
        out Box2i bounds)
    {
        bounds = default;

        if (!planet.ResidenceReservationEnabled)
            return false;

        var size = Math.Max(1, planet.ResidenceReservationSize);
        var min = -size / 2;
        bounds = new Box2i(min, min, min + size, min + size);
        return true;
    }

    private static Box2 ToBox2(Box2i bounds)
    {
        return new Box2(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top);
    }

    private static int GetDistanceToSquareEdge(int x, int y, int halfSize)
    {
        var left = x + halfSize;
        var right = halfSize - 1 - x;
        var bottom = y + halfSize;
        var top = halfSize - 1 - y;

        return Math.Min(Math.Min(left, right), Math.Min(bottom, top));
    }

    private static int GetSquareRingTileCount(int halfSize, int ringWidth)
    {
        if (ringWidth <= 0 || halfSize <= 0)
            return 0;

        var size = halfSize * 2;
        var innerSize = Math.Max(0, size - ringWidth * 2);
        return size * size - innerSize * innerSize;
    }
}
