using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Parallax;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float LoadRange = 16f;
    private readonly Box2 _loadArea = new Box2(-LoadRange, -LoadRange, LoadRange, LoadRange);

    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    public override void Initialize()
    {
        base.Initialize();
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

            if (pSession.AttachedEntity != null &&
                !_handledEntities.Add(pSession.AttachedEntity.Value) &&
                xformQuery.TryGetComponent(pSession.AttachedEntity, out var xform) &&
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

        // Load new chunks
        biomes = EntityQueryEnumerator<BiomeComponent>();

        while (biomes.MoveNext(out var biome))
        {
            _activeChunks.Add(biome, new HashSet<Vector2i>());
        }

        // Unload old chunks

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

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            // Load NOW!
            LoadChunk(component, grid, chunk);
        }
    }

    private void LoadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk)
    {
        // Set tiles first
        // TODO: Pass this in
        var tiles = new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                // If there's existing data then don't overwrite it.
                if (grid.TryGetTileRef(indices, out _))
                    continue;

                // TODO Get tile from biomesystem (move to shared)
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
            if (!active.Contains(chunk))
                continue;

            // Unload NOW!
            UnloadChunk(component, grid, chunk);
        }
    }

    private void UnloadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk)
    {
        // Reverse order to loading
        // Delete entities

        // Unset tiles (if the data is custom)
        // TODO: Pass this in
        var tiles = new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);


                // TODO Get tile from biomesystem (move to shared)
                // If it doesn't match then don't set
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
