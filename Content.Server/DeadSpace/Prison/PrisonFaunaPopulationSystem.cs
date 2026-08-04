using System.Numerics;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.Parallax;
using Content.Server.Tiles;
using Content.Shared.Chasm;
using Content.Shared.DeadSpace.Prison;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Prison;

public sealed class PrisonFaunaPopulationSystem : EntitySystem
{
    private const CollisionGroup SpawnBlockerMask =
        CollisionGroup.Impassable |
        CollisionGroup.HighImpassable |
        CollisionGroup.MidImpassable |
        CollisionGroup.LowImpassable;

    private static readonly Vector2i[] CardinalOffsets =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    ];

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly Dictionary<EntProtoId, int> _counts = new();
    private readonly HashSet<EntityUid> _tileEntities = new();
    private readonly List<(Vector2i Index, Tile Tile)> _reservedTiles = new();
    private readonly List<Vector2i> _expiredSectors = new();
    private List<Entity<MapGridComponent>> _nearbyGrids = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrisonSpawnedFaunaComponent, MobStateChangedEvent>(OnFaunaMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PrisonFaunaPopulationComponent, PrisonMapComponent>();
        while (query.MoveNext(out var mapUid, out var population, out var prisonMap))
        {
            if (population.NextSpawnTime > now ||
                !_prototype.TryIndex<PrisonPlanetPrototype>(prisonMap.Planet, out var planet))
            {
                continue;
            }

            if (!planet.FaunaEnabled || planet.FaunaSpawns.Count == 0)
                continue;

            PruneExpiredCooldowns(population, now);
            var total = CountLiveFauna(mapUid, planet);
            if (population.InitialSpawnRemaining > 0)
            {
                var initialBatch = Math.Min(
                    Math.Max(1, planet.FaunaInitialSpawnBatchSize),
                    population.InitialSpawnRemaining);
                initialBatch = Math.Min(initialBatch, Math.Max(0, planet.FaunaHardCap - total));

                var spawned = SpawnFauna(mapUid, population, planet, initialBatch);
                population.InitialSpawnRemaining -= spawned;
                population.NextSpawnTime = now + GetInitialSpawnInterval(planet);

                if (spawned == 0)
                {
                    Log.Warning("Prison fauna initial spawn batch could not place any mobs; initial seeding stopped.");
                    population.InitialSpawnRemaining = 0;
                }
                else if (population.InitialSpawnRemaining == 0)
                {
                    Log.Info($"Prison fauna initial seeding completed with {total + spawned} live mobs.");
                }

                continue;
            }

            population.NextSpawnTime = now + GetUpdateInterval(planet);
            var target = Math.Min(planet.FaunaSoftCap, planet.FaunaHardCap);
            if (total >= target)
                continue;

            var batchMax = total < planet.FaunaLowPopulationThreshold
                ? Math.Max(planet.FaunaSpawnBatchMax, planet.FaunaLowPopulationBatchMax)
                : planet.FaunaSpawnBatchMax;
            batchMax = Math.Max(0, batchMax);
            if (batchMax == 0)
                continue;

            var batchMin = Math.Clamp(planet.FaunaSpawnBatchMin, 0, batchMax);
            var requested = _random.Next(batchMin, batchMax + 1);
            var batch = Math.Min(requested, target - total);
            if (batch > 0 && SpawnFauna(mapUid, population, planet, batch) == 0)
                Log.Warning($"Prison fauna respawn failed to place {batch} requested mobs.");
        }
    }

    public void SetupMap(EntityUid mapUid, PrisonPlanetPrototype planet)
    {
        if (!planet.FaunaEnabled || planet.FaunaSpawns.Count == 0)
            return;

        var population = EnsureComp<PrisonFaunaPopulationComponent>(mapUid);
        population.SectorCooldowns.Clear();
        population.InitialSpawnRemaining = Math.Min(
            Math.Max(0, planet.FaunaInitialSpawnCount),
            Math.Max(0, planet.FaunaHardCap));
        population.NextSpawnTime = _timing.CurTime + GetInitialSpawnInterval(planet);

        Log.Info($"Prison fauna initial seeding scheduled for {population.InitialSpawnRemaining} mobs.");
    }

    private void OnFaunaMobStateChanged(Entity<PrisonSpawnedFaunaComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead ||
            ent.Comp.Map == EntityUid.Invalid ||
            !TryComp<PrisonFaunaPopulationComponent>(ent.Comp.Map, out var population) ||
            !TryComp<PrisonMapComponent>(ent.Comp.Map, out var prisonMap) ||
            !_prototype.TryIndex<PrisonPlanetPrototype>(prisonMap.Planet, out var planet) ||
            planet.FaunaSectorCooldown <= TimeSpan.Zero)
        {
            return;
        }

        population.SectorCooldowns[ent.Comp.Sector] = _timing.CurTime + planet.FaunaSectorCooldown;
    }

    private int CountLiveFauna(EntityUid mapUid, PrisonPlanetPrototype planet)
    {
        _counts.Clear();
        foreach (var entry in planet.FaunaSpawns)
            _counts.TryAdd(entry.Prototype, 0);

        var total = 0;
        var query = EntityQueryEnumerator<PrisonSpawnedFaunaComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawned, out var mobState, out var xform))
        {
            if (spawned.Map != mapUid || xform.MapUid != mapUid || _mobState.IsDead(uid, mobState))
                continue;

            total++;
            _counts.TryAdd(spawned.Prototype, 0);
            _counts[spawned.Prototype]++;
        }

        return total;
    }

    private int SpawnFauna(
        EntityUid mapUid,
        PrisonFaunaPopulationComponent population,
        PrisonPlanetPrototype planet,
        int requested)
    {
        if (!TryComp<MapGridComponent>(mapUid, out var grid))
            return 0;

        var total = CountLiveFauna(mapUid, planet);
        var spawned = 0;
        var usedSectors = new HashSet<Vector2i>();
        for (var i = 0; i < requested && total < planet.FaunaHardCap; i++)
        {
            if (!TryPickEntry(planet, out var entry) ||
                !TryFindSpawnCoordinates(mapUid, grid, population, planet, usedSectors, out var coordinates, out var sector))
            {
                continue;
            }

            var uid = Spawn(entry.Prototype.Id, coordinates);
            var fauna = EnsureComp<PrisonSpawnedFaunaComponent>(uid);
            fauna.Map = mapUid;
            fauna.Prototype = entry.Prototype;
            fauna.Sector = sector;
            fauna.SentenceReductionMinutes = Math.Max(0, entry.SentenceReductionMinutes);
            usedSectors.Add(sector);

            _counts.TryAdd(entry.Prototype, 0);
            _counts[entry.Prototype]++;
            total++;
            spawned++;
        }

        return spawned;
    }

    private bool TryPickEntry(PrisonPlanetPrototype planet, out PrisonFaunaSpawnEntry entry)
    {
        entry = default!;
        var totalWeight = 0;
        foreach (var candidate in planet.FaunaSpawns)
        {
            if (candidate.Weight <= 0 || candidate.MaxCount <= 0 ||
                !_prototype.HasIndex<EntityPrototype>(candidate.Prototype) ||
                _counts.GetValueOrDefault(candidate.Prototype) >= candidate.MaxCount)
            {
                continue;
            }

            totalWeight += candidate.Weight;
        }

        if (totalWeight <= 0)
            return false;

        var roll = _random.Next(totalWeight);
        foreach (var candidate in planet.FaunaSpawns)
        {
            if (candidate.Weight <= 0 || candidate.MaxCount <= 0 ||
                !_prototype.HasIndex<EntityPrototype>(candidate.Prototype) ||
                _counts.GetValueOrDefault(candidate.Prototype) >= candidate.MaxCount)
            {
                continue;
            }

            roll -= candidate.Weight;
            if (roll >= 0)
                continue;

            entry = candidate;
            return true;
        }

        return false;
    }

    private bool TryFindSpawnCoordinates(
        EntityUid mapUid,
        MapGridComponent grid,
        PrisonFaunaPopulationComponent population,
        PrisonPlanetPrototype planet,
        HashSet<Vector2i> usedSectors,
        out EntityCoordinates coordinates,
        out Vector2i sector)
    {
        coordinates = default;
        sector = default;

        var limit = GetSpawnLimit(planet);
        if (limit <= 0 || !TryComp<BiomeComponent>(mapUid, out var biome))
            return false;

        var mapId = Transform(mapUid).MapID;
        for (var pass = 0; pass < 2; pass++)
        {
            var avoidUsedSectors = pass == 0 && usedSectors.Count > 0;
            for (var attempt = 0; attempt < planet.FaunaSpawnAttempts; attempt++)
            {
                var indices = new Vector2i(_random.Next(-limit, limit + 1), _random.Next(-limit, limit + 1));
                var center = _map.GridTileToLocal(mapUid, grid, indices).Position;
                sector = GetSector(center, planet.FaunaSectorSize);

                if (avoidUsedSectors && usedSectors.Contains(sector) ||
                    population.SectorCooldowns.ContainsKey(sector) ||
                    IsInsideExclusion(center, planet) ||
                    HasNearbyPlayer(mapId, center, planet.FaunaMinPlayerDistance) ||
                    HasNearbyNonTerrainGrid(mapUid, center, Math.Max(2f, planet.FaunaSpawnClearance + 1f)) ||
                    !IsTileSafeForSpawn(mapUid, grid, biome, indices, planet.FaunaSpawnClearance, true) ||
                    !ReserveSpawnArea(mapUid, grid, biome, indices, planet.FaunaSpawnClearance) ||
                    !_map.TryGetTileRef(mapUid, grid, indices, out var tile))
                {
                    continue;
                }

                coordinates = _map.ToCenterCoordinates(tile, grid);
                return true;
            }
        }

        return false;
    }

    private bool ReserveSpawnArea(EntityUid mapUid, MapGridComponent grid, BiomeComponent biome, Vector2i center, int clearance)
    {
        clearance = Math.Max(0, clearance);
        var bounds = new Box2(
            center.X - clearance,
            center.Y - clearance,
            center.X + clearance + 1,
            center.Y + clearance + 1);
        _reservedTiles.Clear();
        _biome.ReserveTiles(mapUid, bounds, _reservedTiles, biome, grid);
        _reservedTiles.Clear();

        return _map.TryGetTileRef(mapUid, grid, center, out var tile) && !tile.Tile.IsEmpty && !_turf.IsSpace(tile);
    }

    private bool IsTileSafeForSpawn(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        Vector2i center,
        int clearance,
        bool checkOccupants)
    {
        clearance = Math.Max(0, clearance);
        for (var x = center.X - clearance; x <= center.X + clearance; x++)
        {
            for (var y = center.Y - clearance; y <= center.Y + clearance; y++)
            {
                if (!IsSingleTilePassable(mapUid, grid, biome, new Vector2i(x, y), checkOccupants))
                    return false;
            }
        }

        var passableNeighbors = 0;
        foreach (var offset in CardinalOffsets)
        {
            if (IsSingleTilePassable(mapUid, grid, biome, center + offset, false))
                passableNeighbors++;
        }

        return passableNeighbors >= 2;
    }

    private bool IsSingleTilePassable(
        EntityUid gridUid,
        MapGridComponent grid,
        BiomeComponent biome,
        Vector2i indices,
        bool checkOccupants)
    {
        if (!_biome.TryGetBiomeTile(indices, biome.Layers, biome.Seed, (gridUid, grid), out var tile) ||
            tile.Value.IsEmpty || _turf.IsSpace(tile.Value) ||
            _biome.TryGetEntity(indices, biome, (gridUid, grid), out _) ||
            _turf.IsTileBlocked(gridUid, indices, SpawnBlockerMask, grid: grid))
        {
            return false;
        }

        return !HasUnsafeEntityInTile(gridUid, grid, indices, checkOccupants);
    }

    private bool HasUnsafeEntityInTile(EntityUid gridUid, MapGridComponent grid, Vector2i indices, bool checkOccupants)
    {
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tile))
            return false;

        _tileEntities.Clear();
        _lookup.GetEntitiesInTile(tile, _tileEntities, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);
        foreach (var uid in _tileEntities)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (HasComp<ChasmComponent>(uid) || HasComp<TileEntityEffectComponent>(uid) ||
                checkOccupants && (HasComp<MobStateComponent>(uid) || HasComp<PrisonSpawnedFaunaComponent>(uid)))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasNearbyPlayer(MapId mapId, Vector2 center, float range)
    {
        if (range <= 0f)
            return false;

        var coordinates = new MapCoordinates(center, mapId);
        foreach (var (uid, _) in _lookup.GetEntitiesInRange<ActorComponent>(coordinates, range))
        {
            if (TerminatingOrDeleted(uid) || HasComp<GhostComponent>(uid) ||
                TryComp<MobStateComponent>(uid, out var state) && _mobState.IsDead(uid, state))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool HasNearbyNonTerrainGrid(EntityUid mapUid, Vector2 center, float radius)
    {
        _nearbyGrids.Clear();
        var bounds = Box2.CenteredAround(center, Vector2.One * (radius * 2f + 1f));
        _mapManager.FindGridsIntersecting(Transform(mapUid).MapID, bounds, ref _nearbyGrids);
        foreach (var grid in _nearbyGrids)
        {
            if (grid.Owner != mapUid)
                return true;
        }

        return false;
    }

    private static bool IsInsideExclusion(Vector2 center, PrisonPlanetPrototype planet)
    {
        if (planet.ResidenceReservationEnabled)
        {
            var halfSize = Math.Max(1, planet.ResidenceReservationSize) / 2f +
                           Math.Max(0f, planet.FaunaResidenceExclusionPadding);
            if (MathF.Abs(center.X) <= halfSize && MathF.Abs(center.Y) <= halfSize)
                return true;
        }

        return planet.FaunaLandingExclusionRadius > 0f &&
               Vector2.DistanceSquared(center, planet.FtlBeaconOffset) <=
               planet.FaunaLandingExclusionRadius * planet.FaunaLandingExclusionRadius;
    }

    private static int GetSpawnLimit(PrisonPlanetPrototype planet)
    {
        var boundaryPadding = planet.BoundaryEnabled ? Math.Max(1, planet.BoundaryWallWidth) : 0;
        var padding = MathF.Ceiling(Math.Max(0f, planet.FaunaMapEdgePadding)) + boundaryPadding;
        return Math.Max(0, planet.MapHalfSize - (int) padding);
    }

    private static Vector2i GetSector(Vector2 center, int sectorSize)
    {
        sectorSize = Math.Max(1, sectorSize);
        return new Vector2i(
            (int) MathF.Floor(center.X / sectorSize),
            (int) MathF.Floor(center.Y / sectorSize));
    }

    private void PruneExpiredCooldowns(PrisonFaunaPopulationComponent population, TimeSpan now)
    {
        _expiredSectors.Clear();
        foreach (var (sector, end) in population.SectorCooldowns)
        {
            if (end <= now)
                _expiredSectors.Add(sector);
        }

        foreach (var sector in _expiredSectors)
            population.SectorCooldowns.Remove(sector);
    }

    private static TimeSpan GetUpdateInterval(PrisonPlanetPrototype planet)
    {
        return planet.FaunaUpdateInterval > TimeSpan.Zero
            ? planet.FaunaUpdateInterval
            : TimeSpan.FromSeconds(90);
    }

    private static TimeSpan GetInitialSpawnInterval(PrisonPlanetPrototype planet)
    {
        return planet.FaunaInitialSpawnInterval > TimeSpan.Zero
            ? planet.FaunaInitialSpawnInterval
            : TimeSpan.FromSeconds(5);
    }
}
