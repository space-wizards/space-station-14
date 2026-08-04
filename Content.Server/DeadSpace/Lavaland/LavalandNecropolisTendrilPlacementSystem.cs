using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Tiles;
using Content.Shared.Chasm;
using Content.Shared.DeadSpace.Lavaland;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class LavalandNecropolisTendrilPlacementSystem : EntitySystem
{
    private const int InitialSpawnBatchSize = 2;
    private const float FtlLandingAreaPadding = 16f;

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

    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly Dictionary<EntProtoId, int> _counts = new();
    private readonly List<Vector2i> _placed = new();
    private readonly HashSet<EntityUid> _tileEntities = new();
    private readonly List<(Vector2i Index, Tile Tile)> _reservedTiles = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LavalandTendrilPopulationComponent, LavalandMapComponent>();
        while (query.MoveNext(out var mapUid, out var population, out var lavaland))
        {
            if (!_prototype.TryIndex<LavalandPlanetPrototype>(lavaland.Planet, out var planet))
                continue;

            var interval = GetRespawnInterval(planet);
            if (interval <= TimeSpan.Zero ||
                population.NextRespawnTime > now ||
                !planet.TendrilsEnabled ||
                planet.TendrilSpawns.Count == 0 ||
                planet.TendrilSpawnCount <= 0)
            {
                continue;
            }

            population.NextRespawnTime = now + interval;

            var live = CountLiveTendrils(mapUid, planet);
            if (live >= planet.TendrilSpawnCount)
                continue;

            var maxBatch = Math.Clamp(planet.TendrilRespawnBatchMax, 0, planet.TendrilSpawnCount - live);
            if (maxBatch <= 0)
                continue;

            var minBatch = Math.Clamp(planet.TendrilRespawnBatchMin, 0, maxBatch);
            var requested = _random.Next(minBatch, maxBatch + 1);
            if (requested <= 0)
                continue;

            var spawned = SpawnTendrils(mapUid, planet, requested);
            if (spawned < requested)
                Log.Warning($"Lavaland necropolis tendril respawn only placed {spawned}/{requested}.");
        }
    }

    public async Task SetupMap(EntityUid mapUid, LavalandPlanetPrototype planet, CancellationToken cancellation)
    {
        if (!planet.TendrilsEnabled ||
            planet.TendrilSpawnCount <= 0 ||
            planet.TendrilSpawns.Count == 0)
        {
            return;
        }

        var population = EnsureComp<LavalandTendrilPopulationComponent>(mapUid);
        var interval = GetRespawnInterval(planet);
        population.NextRespawnTime = interval > TimeSpan.Zero
            ? _timing.CurTime + interval
            : TimeSpan.Zero;

        _counts.Clear();
        _placed.Clear();

        var attempted = 0;
        var spawned = 0;
        while (attempted < planet.TendrilSpawnCount)
        {
            cancellation.ThrowIfCancellationRequested();
            var batch = Math.Min(InitialSpawnBatchSize, planet.TendrilSpawnCount - attempted);
            var placed = SpawnTendrils(mapUid, planet, batch);
            attempted += batch;
            spawned += placed;

            if (attempted < planet.TendrilSpawnCount)
                await Timer.Delay(1, cancellation);
        }

        Log.Info($"Lavaland necropolis tendrils placed {spawned}/{planet.TendrilSpawnCount}.");

        if (spawned < planet.TendrilSpawnCount)
            Log.Warning($"Lavaland necropolis tendrils only placed {spawned}/{planet.TendrilSpawnCount}.");
    }

    private int SpawnTendrils(EntityUid mapUid, LavalandPlanetPrototype planet, int count)
    {
        if (count <= 0 ||
            !TryComp<MapGridComponent>(mapUid, out var grid) ||
            !TryComp<BiomeComponent>(mapUid, out var biome))
        {
            return 0;
        }

        var spawned = 0;
        for (var i = 0; i < count; i++)
        {
            if (!TryPickEntry(planet, out var entry) ||
                !TryFindPosition(mapUid, grid, biome, planet, _placed, out var indices))
            {
                continue;
            }

            ReserveArea(mapUid, grid, biome, indices, planet.TendrilClearRadius);

            if (!_map.TryGetTileRef(mapUid, grid, indices, out var tile))
                continue;

            var uid = Spawn(entry.Prototype.Id, _map.ToCenterCoordinates(tile, grid));
            EnsureComp<LavalandNecropolisTendrilComponent>(uid);

            _placed.Add(indices);
            _counts.TryAdd(entry.Prototype, 0);
            _counts[entry.Prototype]++;
            spawned++;
        }

        return spawned;
    }

    private int CountLiveTendrils(EntityUid mapUid, LavalandPlanetPrototype planet)
    {
        _counts.Clear();
        _placed.Clear();

        if (!TryComp<MapGridComponent>(mapUid, out var grid))
            return 0;

        var mapId = Transform(mapUid).MapID;
        var live = 0;
        var query = EntityQueryEnumerator<LavalandNecropolisTendrilComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tendril, out var xform))
        {
            if (tendril.Destroyed ||
                TerminatingOrDeleted(uid) ||
                xform.MapID != mapId ||
                MetaData(uid).EntityPrototype?.ID is not { } prototypeId)
            {
                continue;
            }

            var prototype = new EntProtoId(prototypeId);
            if (!IsTendrilSpawnPrototype(planet, prototype))
                continue;

            _counts.TryAdd(prototype, 0);
            _counts[prototype]++;

            if (_map.TryGetTileRef(mapUid, grid, xform.Coordinates, out var tile))
                _placed.Add(tile.GridIndices);

            live++;
        }

        return live;
    }

    private static bool IsTendrilSpawnPrototype(LavalandPlanetPrototype planet, EntProtoId prototype)
    {
        foreach (var entry in planet.TendrilSpawns)
        {
            if (entry.Prototype == prototype)
                return true;
        }

        return false;
    }

    private bool TryPickEntry(LavalandPlanetPrototype planet, out LavalandTendrilSpawnEntry entry)
    {
        entry = default!;

        var totalWeight = 0;
        foreach (var candidate in planet.TendrilSpawns)
        {
            if (candidate.Weight <= 0 ||
                candidate.MaxCount <= 0 ||
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
        foreach (var candidate in planet.TendrilSpawns)
        {
            if (candidate.Weight <= 0 ||
                candidate.MaxCount <= 0 ||
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

    private bool TryFindPosition(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        LavalandPlanetPrototype planet,
        List<Vector2i> placed,
        out Vector2i indices)
    {
        indices = default;

        var limit = GetSpawnLimit(planet);
        if (limit <= 0)
            return false;

        var minDistance = Math.Min(Math.Max(0, planet.TendrilMinDistance), limit);
        var maxDistance = Math.Clamp(planet.TendrilMaxDistance, minDistance, limit);
        var minSeparationSquared = planet.TendrilMinSeparation * planet.TendrilMinSeparation;

        for (var attempt = 0; attempt < planet.TendrilSpawnAttempts; attempt++)
        {
            var angle = _random.NextFloat(MathF.PI * 2f);
            var distance = _random.NextFloat(minDistance, maxDistance);
            var candidate = new Vector2i(
                (int) MathF.Round(MathF.Cos(angle) * distance),
                (int) MathF.Round(MathF.Sin(angle) * distance));

            if (IsInsideExclusion(candidate, planet) ||
                !IsSeparated(candidate, placed, minSeparationSquared) ||
                !IsTileSafe(mapUid, grid, biome, candidate))
            {
                continue;
            }

            indices = candidate;
            return true;
        }

        return false;
    }

    private bool IsTileSafe(EntityUid mapUid, MapGridComponent grid, BiomeComponent biome, Vector2i indices)
    {
        if (!IsSingleTileSafe(mapUid, grid, biome, indices))
            return false;

        var passableNeighbors = 0;
        foreach (var offset in CardinalOffsets)
        {
            if (IsSingleTileSafe(mapUid, grid, biome, indices + offset))
                passableNeighbors++;
        }

        return passableNeighbors >= 2;
    }

    private bool IsSingleTileSafe(EntityUid mapUid, MapGridComponent grid, BiomeComponent biome, Vector2i indices)
    {
        if (!_biome.TryGetBiomeTile(indices, biome.Layers, biome.Seed, (mapUid, grid), out var tile) ||
            tile.Value.IsEmpty ||
            _turf.IsSpace(tile.Value) ||
            _biome.TryGetEntity(indices, biome, (mapUid, grid), out _) ||
            _turf.IsTileBlocked(mapUid, indices, SpawnBlockerMask, grid: grid))
        {
            return false;
        }

        return !HasUnsafeEntityInTile(mapUid, grid, indices);
    }

    private bool HasUnsafeEntityInTile(EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tile))
            return false;

        _tileEntities.Clear();
        _lookup.GetEntitiesInTile(tile, _tileEntities, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);

        foreach (var uid in _tileEntities)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (HasComp<ChasmComponent>(uid) ||
                HasComp<TileEntityEffectComponent>(uid) ||
                HasComp<LavalandOutpostComponent>(uid) ||
                HasComp<LavalandShelterComponent>(uid) ||
                HasComp<LavalandNecropolisTendrilComponent>(uid) ||
                HasComp<MobStateComponent>(uid))
            {
                return true;
            }
        }

        return false;
    }

    private void ReserveArea(
        EntityUid mapUid,
        MapGridComponent grid,
        BiomeComponent biome,
        Vector2i center,
        int radius)
    {
        radius = Math.Max(0, radius);

        var min = new Vector2(center.X - radius, center.Y - radius);
        var max = new Vector2(center.X + radius + 1, center.Y + radius + 1);

        _reservedTiles.Clear();
        _biome.ReserveTiles(mapUid, new Box2(min, max), _reservedTiles, biome, grid);
        _reservedTiles.Clear();
    }

    private static bool IsSeparated(Vector2i candidate, List<Vector2i> placed, int minSeparationSquared)
    {
        foreach (var other in placed)
        {
            if ((candidate - other).LengthSquared < minSeparationSquared)
                return false;
        }

        return true;
    }

    private static bool IsInsideExclusion(Vector2i indices, LavalandPlanetPrototype planet)
    {
        var center = new Vector2(indices.X, indices.Y);

        if (planet.TendrilLandingExclusionRadius > 0f &&
            center.LengthSquared() <= planet.TendrilLandingExclusionRadius * planet.TendrilLandingExclusionRadius)
        {
            return true;
        }

        if (planet.TerminalReservationEnabled &&
            planet.TendrilOutpostExclusionRadius > 0f &&
            Vector2.DistanceSquared(center, planet.TerminalGridOffset) <=
            planet.TendrilOutpostExclusionRadius * planet.TendrilOutpostExclusionRadius)
        {
            return true;
        }

        var ftlRadius = GetFtlBeaconExclusionRadius(planet.TendrilFtlBeaconExclusionRadius, planet);
        if (ftlRadius > 0f &&
            Vector2.DistanceSquared(center, planet.FtlBeaconOffset) <= ftlRadius * ftlRadius)
        {
            return true;
        }

        return false;
    }

    private static float GetFtlBeaconExclusionRadius(float configuredRadius, LavalandPlanetPrototype planet)
    {
        if (!planet.FtlEnabled)
            return 0f;

        return Math.Max(configuredRadius, Math.Max(0f, planet.FtlFallbackMaxOffset) + FtlLandingAreaPadding);
    }

    private static int GetSpawnLimit(LavalandPlanetPrototype planet)
    {
        if (planet.MapHalfSize <= 0)
            return 0;

        var boundaryPadding = planet.BoundaryEnabled
            ? Math.Max(0, planet.BoundaryLavaWidth) + Math.Max(1, planet.BoundaryWallWidth)
            : 0;

        var padding = MathF.Ceiling(Math.Max(0f, planet.TendrilMapEdgePadding)) + boundaryPadding;
        return Math.Max(1, planet.MapHalfSize - (int) padding);
    }

    private static TimeSpan GetRespawnInterval(LavalandPlanetPrototype planet)
    {
        if (planet.TendrilRespawnInterval <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return TimeSpan.FromSeconds(Math.Max(60, planet.TendrilRespawnInterval.TotalSeconds));
    }
}
