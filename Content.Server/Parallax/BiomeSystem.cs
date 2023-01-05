using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Parallax;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ISawmill _sawmill = default!;
    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float LoadRange = ChunkSize * 1.5f;
    private readonly Box2 _loadArea = new Box2(-LoadRange, -LoadRange, LoadRange, LoadRange);

    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("biome");
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        component.Seed = _random.Next();
        Dirty(component);
    }

    // TODO: Need a way to designate an area as preload (i.e. keep it loaded until someone goes nearby)

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var biomeQuery = GetEntityQuery<BiomeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var biomes = EntityQueryEnumerator<BiomeComponent>();

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
                AddChunksInRange(biome, xform.WorldPosition);
            }

            foreach (var viewer in pSession.ViewSubscriptions)
            {
                if (!_handledEntities.Add(viewer) ||
                    !xformQuery.TryGetComponent(viewer, out xform) ||
                    !biomeQuery.TryGetComponent(xform.MapUid, out biome))
                {
                    continue;
                }

                AddChunksInRange(biome, xform.WorldPosition);
            }
        }

        var loadBiomes = EntityQueryEnumerator<BiomeComponent, MapGridComponent>();

        while (loadBiomes.MoveNext(out var biome, out var grid))
        {
            // Load new chunks
            LoadChunks(biome, grid);
            // Unload old chunks
            UnloadChunks(biome, grid);
        }

        _handledEntities.Clear();
        _activeChunks.Clear();
    }

    private void AddChunksInRange(BiomeComponent biome, Vector2 worldPos)
    {
        var enumerator = new ChunkIndicesEnumerator(_loadArea.Translated(worldPos), ChunkSize);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            _activeChunks[biome].Add(chunkOrigin.Value);
        }
    }

    private void LoadChunks(BiomeComponent component, MapGridComponent grid)
    {
        var active = _activeChunks[component];
        var noise = new FastNoise(component.Seed);
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            // Load NOW!
            LoadChunk(component, grid, chunk * ChunkSize, noise, prototype);
        }
    }

    private void LoadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk, FastNoise noise, BiomePrototype prototype)
    {
        _sawmill.Debug($"Loading chunk for {ToPrettyString(component.Owner)} at {chunk}");

        // Set tiles first
        // TODO: Pass this in
        var tiles = new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

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

        // Now do entities
    }

    private void UnloadChunks(BiomeComponent component, MapGridComponent grid)
    {
        var active = _activeChunks[component];

        foreach (var chunk in component.LoadedChunks)
        {
            if (active.Contains(chunk) || !component.LoadedChunks.Remove(chunk))
                continue;

            // Unload NOW!
            UnloadChunk(component, grid, chunk * ChunkSize);
        }
    }

    private void UnloadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk)
    {
        _sawmill.Debug($"Unloading chunk for {ToPrettyString(component.Owner)} at {chunk}");
        // Reverse order to loading
        // Delete entities

        // Unset tiles (if the data is custom)
        // TODO: Pass this in
        var tiles = new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
        var noise = new FastNoise(component.Seed);
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                // If it's default data unload the tile.
                if (!TryGetBiomeTile(indices, prototype, noise, null, out var biomeTile) || grid.TryGetTileRef(indices, out var tileRef) && tileRef.Tile != biomeTile.Value)
                    continue;

                tiles.Add((indices, Tile.Empty));
            }
        }

        grid.SetTiles(tiles);
    }

    // TODO: Load if in range, unload if out of range.
    // TODO: Round the view range
    // For unloading diff entities

    // TODO: For loading
    // - Load tiles if no data exists I think, probably better
}
