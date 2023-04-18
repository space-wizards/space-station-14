using Content.Server.Decals;
using Content.Server.Procedural;
using Content.Server.Shuttles.Events;
using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Parallax.Biomes.Points;
using Content.Shared.Procedural;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float DefaultLoadRange = 16f;
    private float _loadRange = DefaultLoadRange;

    /// <summary>
    /// Load area for chunks containing tiles, decals etc.
    /// </summary>
    private Box2 _loadArea = new(-DefaultLoadRange, -DefaultLoadRange, DefaultLoadRange, DefaultLoadRange);

    /// <summary>
    /// Stores the chunks active for this tick temporarily.
    /// </summary>
    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    private readonly Dictionary<BiomeComponent,
        Dictionary<IBiomeMarkerLayer,
        Dictionary<string, HashSet<Vector2i>>>> _markerChunks = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentStartup>(OnBiomeStartup);
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, SetLoadRange, true);
        InitializePoints();
        _proto.PrototypesReloaded += ProtoReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, SetLoadRange);
        _proto.PrototypesReloaded -= ProtoReload;
    }

    private void ProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(BiomeTemplatePrototype), out var reloads))
            return;

        var query = AllEntityQuery<BiomeComponent>();

        while (query.MoveNext(out var biome))
        {
            if (biome.Template == null || !reloads.Modified.TryGetValue(biome.Template, out var proto))
                continue;

            SetTemplate(biome, (BiomeTemplatePrototype) proto);
        }
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
        // TODO: SHITCODE FIX
        // Also make it so the biomeprototype is just a template to use
        // Any templates based off of it should be dynamically reloaded too
        component.MobMarkerLayers.Add("Lizards");
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        SetSeed(component, _random.Next());
    }

    public void SetPrototype(BiomeComponent component, string proto)
    {
        if (component.Template == proto)
            return;

        component.Template = proto;
        Dirty(component);
    }

    public void SetSeed(BiomeComponent component, int seed)
    {
        component.Seed = seed;
        component.Noise.SetSeed(seed);
        Dirty(component);
    }

    /// <summary>
    /// Sets the <see cref="BiomeComponent.Template"/> and refreshes layers.
    /// </summary>
    public void SetTemplate(BiomeComponent component, BiomeTemplatePrototype template)
    {
        component.Layers.Clear();
        component.Template = template.ID;

        foreach (var layer in template.Layers)
        {
            component.Layers.Add(layer);
        }

        Dirty(component);
    }

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        var targetMap = ev.TargetCoordinates.ToMap(EntityManager, _transform);
        var targetMapUid = _mapManager.GetMapEntityId(targetMap.MapId);

        if (!TryComp<BiomeComponent>(targetMapUid, out var biome))
            return;

        var targetArea = new Box2(targetMap.Position - 64f, targetMap.Position + 64f);
        Preload(targetMapUid, biome, targetArea);
    }

    /// <summary>
    /// Preloads biome for the specified area.
    /// </summary>
    public void Preload(EntityUid uid, BiomeComponent component, Box2 area)
    {
        var mobs = component.MobMarkerLayers;
        var goobers = _markerChunks.GetOrNew(component);

        foreach (var layer in mobs)
        {
            var proto = _proto.Index<BiomeMobMarkerLayerPrototype>(layer);
            var enumerator = new ChunkIndicesEnumerator(area, proto.Size);

            while (enumerator.MoveNext(out var chunk))
            {
                var chunkOrigin = chunk * proto.Size;
                var lay = goobers.GetOrNew(proto);
                var layerChunks = lay.GetOrNew(proto.ID);
                layerChunks.Add(chunkOrigin.Value * proto.Size);
            }
        }

        var dungeons = component.DungeonMarkerLayers;
        foreach (var layer in dungeons)
        {
            var proto = _proto.Index<BiomeDungeonMarkerLayerPrototype>(layer);
            var enumerator = new ChunkIndicesEnumerator(area, proto.Size);

            while (enumerator.MoveNext(out var chunk))
            {
                var chunkOrigin = chunk * proto.Size;
                var lay = goobers.GetOrNew(proto);
                var layerChunks = lay.GetOrNew(proto.ID);
                layerChunks.Add(chunkOrigin.Value * proto.Size);
            }
        }
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
            _markerChunks.GetOrNew(biome);
        }

        // Get chunks in range
        foreach (var client in Filter.GetAllPlayers(_playerManager))
        {
            var pSession = (IPlayerSession) client;

            if (xformQuery.TryGetComponent(pSession.AttachedEntity, out var xform) &&
                _handledEntities.Add(pSession.AttachedEntity.Value) &&
                 biomeQuery.TryGetComponent(xform.MapUid, out var biome))
            {
                var worldPos = _transform.GetWorldPosition(xform, xformQuery);
                AddChunksInRange(biome, worldPos);

                foreach (var layer in biome.MobMarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeMobMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }

                foreach (var layer in biome.DungeonMarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeDungeonMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }
            }

            foreach (var viewer in pSession.ViewSubscriptions)
            {
                if (!_handledEntities.Add(viewer) ||
                    !xformQuery.TryGetComponent(viewer, out xform) ||
                    !biomeQuery.TryGetComponent(xform.MapUid, out biome))
                {
                    continue;
                }

                var worldPos = _transform.GetWorldPosition(xform, xformQuery);
                AddChunksInRange(biome, worldPos);

                foreach (var layer in biome.MobMarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeMobMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }

                foreach (var layer in biome.DungeonMarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeDungeonMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }
            }
        }

        var loadBiomes = AllEntityQuery<BiomeComponent, MapGridComponent>();

        while (loadBiomes.MoveNext(out var gridUid, out var biome, out var grid))
        {
            var noise = biome.Noise;

            // Load new chunks
            LoadChunks(biome, gridUid, grid, noise, xformQuery);
            // Unload old chunks
            UnloadChunks(biome, gridUid, grid, noise);
        }

        _handledEntities.Clear();
        _activeChunks.Clear();
        _markerChunks.Clear();
    }

    private void AddChunksInRange(BiomeComponent biome, Vector2 worldPos)
    {
        var enumerator = new ChunkIndicesEnumerator(_loadArea.Translated(worldPos), ChunkSize);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            _activeChunks[biome].Add(chunkOrigin.Value * ChunkSize);
        }
    }

    private void AddMarkerChunksInRange(BiomeComponent biome, Vector2 worldPos, IBiomeMarkerLayer layer)
    {
        // Offset the load area so it's centralised.
        var loadArea = new Box2(0, 0, layer.Size, layer.Size);
        var enumerator = new ChunkIndicesEnumerator(loadArea.Translated(worldPos - layer.Size / 2f), layer.Size);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            var lay = _markerChunks[biome].GetOrNew(layer);
            var layerChunks = lay.GetOrNew(layer.ID);
            layerChunks.Add(chunkOrigin.Value * layer.Size);
        }
    }

    private void LoadChunks(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        FastNoiseLite noise,
        EntityQuery<TransformComponent> xformQuery)
    {
        var markers = _markerChunks[component];

        foreach (var (layerType, mark) in markers)
        {
            Dictionary<string, HashSet<Vector2i>> loadedMarkers;

            switch (layerType)
            {
                case BiomeDungeonMarkerLayerPrototype:
                    loadedMarkers = component.LoadedDungeonMarkers;
                    break;
                case BiomeMobMarkerLayerPrototype:
                    loadedMarkers = component.LoadedMobMarkers;
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var (layer, chunks) in mark)
            {
                foreach (var chunk in chunks)
                {
                    if (loadedMarkers.TryGetValue(layer, out var mobChunks) && mobChunks.Contains(chunk))
                        continue;

                    // TODO: Need buffer region for dungeons around chunks.
                    var layerProto = (IBiomeMarkerLayer) _proto.Index(layerType.GetType(), layer);
                    mobChunks ??= new HashSet<Vector2i>();
                    mobChunks.Add(chunk);
                    loadedMarkers[layer] = mobChunks;

                    // Load the lizzers NOW
                    // TODO: Poisson disk

                    switch (layerProto)
                    {
                        case BiomeDungeonMarkerLayerPrototype dunProto:
                            for (var i = 0; i < layerProto.Count; i++)
                            {
                                var point = new Vector2i(_random.Next(chunk.X, chunk.X + layerProto.Size), _random.Next(chunk.Y, chunk.Y + layerProto.Size));
                                _dungeon.GenerateDungeon(_proto.Index<DungeonConfigPrototype>(dunProto.Prototype), gridUid, grid, point, _random.Next());
                            }
                            break;
                        case BiomeMobMarkerLayerPrototype mobProto:
                            for (var i = 0; i < layerProto.Count; i++)
                            {
                                var point = new Vector2(_random.Next(chunk.X, chunk.X + layerProto.Size), _random.Next(chunk.Y, chunk.Y + layerProto.Size));

                                for (var j = 0; j < mobProto.GroupCount; j++)
                                {
                                    Spawn(mobProto.Prototype, new EntityCoordinates(gridUid, point));
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        var active = _activeChunks[component];
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            // Load NOW!
            LoadChunk(component, gridUid, grid, chunk, noise, tiles, xformQuery);
        }
    }

    private void LoadChunk(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        FastNoiseLite noise,
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
                if (!TryGetBiomeTile(indices, component.Layers, noise, null, out var biomeTile) || biomeTile.Value == tileRef.Tile)
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

                if (anchored.MoveNext(out _) || !TryGetEntity(indices, component.Layers, noise, grid, out var entPrototype))
                    continue;

                // TODO: Fix non-anchored ents spawning.
                // Just track loaded chunks for now.
                var ent = Spawn(entPrototype, grid.GridTileToLocal(indices));

                // At least for now unless we do lookups or smth, only work with anchoring.
                if (xformQuery.TryGetComponent(ent, out var xform) && !xform.Anchored)
                {
                    _transform.AnchorEntity(ent, xform, gridUid, grid, indices);
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

                if (anchored.MoveNext(out _) || !TryGetDecals(indices, component.Layers, noise, grid, out var decals))
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
                if (!TryGetBiomeTile(indices, component.Layers, noise, null, out var biomeTile) ||
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
