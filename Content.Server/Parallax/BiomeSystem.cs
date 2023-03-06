using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Parallax;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float DefaultLoadRange = 16f;
    private float _loadRange = DefaultLoadRange;
    private Box2 _loadArea = new(-DefaultLoadRange, -DefaultLoadRange, DefaultLoadRange, DefaultLoadRange);

    /// <summary>
    /// Stores the chunks active for this tick temporarily.
    /// </summary>
    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentStartup>(OnBiomeStartup);
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, SetLoadRange, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, SetLoadRange);
    }

    private void SetLoadRange(float obj)
    {
        // Round it up
        _loadRange = MathF.Ceiling(obj / ChunkSize) * ChunkSize;
        _loadArea = new Box2(-_loadRange, -_loadRange, _loadRange, _loadRange);
    }

    private void OnBiomeStartup(EntityUid uid, BiomeComponent component, ComponentStartup args)
    {
        component.Noise.SetSeed(component.Seed);
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        component.Seed = _random.Next();
        component.Noise.SetSeed(component.Seed);
        Dirty(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var biomeQuery = GetEntityQuery<BiomeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var biomes = AllEntityQuery<BiomeComponent>();

        while (biomes.MoveNext(out var biome))
        {
            _activeChunks.Add(biome, new HashSet<Vector2i>());
        }

        // Get chunks in range
        foreach (var client in Filter.GetAllPlayers(_playerManager))
        {
            var pSession = (IPlayerSession) client;

            if (xformQuery.TryGetComponent(pSession.AttachedEntity, out var xform) &&
                _handledEntities.Add(pSession.AttachedEntity.Value) &&
                 biomeQuery.TryGetComponent(xform.MapUid, out var biome))
            {
                AddChunksInRange(biome, _transform.GetWorldPosition(xform, xformQuery));
            }

            foreach (var viewer in pSession.ViewSubscriptions)
            {
                if (!_handledEntities.Add(viewer) ||
                    !xformQuery.TryGetComponent(viewer, out xform) ||
                    !biomeQuery.TryGetComponent(xform.MapUid, out biome))
                {
                    continue;
                }

                AddChunksInRange(biome, _transform.GetWorldPosition(xform, xformQuery));
            }
        }

        var loadBiomes = AllEntityQuery<BiomeComponent, MapGridComponent>();

        while (loadBiomes.MoveNext(out var biome, out var grid))
        {
            var noise = biome.Noise;
            var gridUid = grid.Owner;

            // Load new chunks
            LoadChunks(biome, gridUid, grid, noise, xformQuery);
            // Unload old chunks
            UnloadChunks(biome, gridUid, grid, noise);
        }

        _handledEntities.Clear();
        _activeChunks.Clear();
    }

    private void AddChunksInRange(BiomeComponent biome, Vector2 worldPos)
    {
        var enumerator = new ChunkIndicesEnumerator(_loadArea.Translated(worldPos), ChunkSize);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            _activeChunks[biome].Add(chunkOrigin.Value * ChunkSize);
        }
    }

    private void LoadChunks(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        FastNoiseLite noise,
        EntityQuery<TransformComponent> xformQuery)
    {
        var active = _activeChunks[component];
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            // Load NOW!
            LoadChunk(component, gridUid, grid, chunk, noise, prototype, tiles, xformQuery);
        }
    }

    private void LoadChunk(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        FastNoiseLite noise,
        BiomePrototype prototype,
        List<(Vector2i, Tile)> tiles,
        EntityQuery<TransformComponent> xformQuery)
    {
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= new HashSet<Vector2i>();

        // Set tiles first
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // If there's existing data then don't overwrite it.
                if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                    continue;

                // Pass in null so we don't try to get the tileref.
                if (!TryGetBiomeTile(indices, prototype, noise, null, out var biomeTile) || biomeTile.Value == tileRef.Tile)
                    continue;

                tiles.Add((indices, biomeTile.Value));
            }
        }

        grid.SetTiles(tiles);
        tiles.Clear();

        // Now do entities
        var loadedEntities = new List<EntityUid>();
        component.LoadedEntities.Add(chunk, loadedEntities);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetEntity(indices, prototype, noise, grid, out var entPrototype))
                    continue;

                // TODO: Fix non-anchored ents spawning.
                // Just track loaded chunks for now.
                var ent = Spawn(entPrototype, grid.GridTileToLocal(indices));

                // At least for now unless we do lookups or smth, only work with anchoring.
                if (xformQuery.TryGetComponent(ent, out var xform) && !xform.Anchored)
                {
                    _transform.AnchorEntity(ent, xform, grid, indices);
                }

                loadedEntities.Add(ent);
            }
        }

        // Decals
        var loadedDecals = new Dictionary<uint, Vector2i>();
        component.LoadedDecals.Add(chunk, loadedDecals);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetDecals(indices, prototype, noise, grid, out var decals))
                    continue;

                foreach (var decal in decals)
                {
                    if (!_decals.TryAddDecal(decal.ID, new EntityCoordinates(gridUid, decal.Position), out var dec))
                        continue;

                    loadedDecals.Add(dec, indices);
                }
            }
        }

        if (modified.Count == 0)
        {
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }

    private void UnloadChunks(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, FastNoiseLite noise)
    {
        var active = _activeChunks[component];
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in component.LoadedChunks)
        {
            if (active.Contains(chunk) || !component.LoadedChunks.Remove(chunk))
                continue;

            // Unload NOW!
            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            UnloadChunk(component, gridUid, grid, chunk, noise, tiles);
        }
    }

    private void UnloadChunk(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i chunk, FastNoiseLite noise, List<(Vector2i, Tile)> tiles)
    {
        // Reverse order to loading
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= new HashSet<Vector2i>();

        // Delete decals
        foreach (var (dec, indices) in component.LoadedDecals[chunk])
        {
            // If we couldn't remove it then flag the tile to never be touched.
            if (!_decals.RemoveDecal(gridUid, dec))
            {
                modified.Add(indices);
            }
        }

        component.LoadedDecals.Remove(chunk);

        // Delete entities
        // This is a TODO
        // Ideally any entities that aren't modified just get deleted and re-generated later
        // This is because if we want to save the map (e.g. persistent server) it makes the file much smaller
        // and also if the map is enormous will make stuff like physics broadphase much faster
        // For now we'll just leave them because no entity diffs.

        component.LoadedEntities.Remove(chunk);

        // Unset tiles (if the data is custom)

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _))
                {
                    modified.Add(indices);
                    continue;
                }

                // If it's default data unload the tile.
                if (!TryGetBiomeTile(indices, prototype, noise, null, out var biomeTile) ||
                    grid.TryGetTileRef(indices, out var tileRef) && tileRef.Tile != biomeTile.Value)
                {
                    modified.Add(indices);
                    continue;
                }

                tiles.Add((indices, Tile.Empty));
            }
        }

        grid.SetTiles(tiles);
        tiles.Clear();
        component.LoadedChunks.Remove(chunk);

        if (modified.Count == 0)
        {
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }
}
