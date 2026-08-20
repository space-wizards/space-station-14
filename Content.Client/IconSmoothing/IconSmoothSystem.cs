using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Utility;

namespace Content.Client.IconSmoothing;

/// <summary>
/// This handles the inner workings of <see cref="IconSmoothComponent"/>
/// TODO: Have this inherit from a generic SpriteSmoothSystemT :P
/// </summary>
/// <remarks>
/// Make no mistake from the commit history. This system was so bad that I just rewrote it from scratch and blew up the old system.
/// IconSmooth is one of the original sins from 2019, and worse is that its tentacles stretch into the renderer for some godforsaken reason.
/// If you can make this more generic, do so please! I wasted way too many hours rewriting this.
/// </remarks>
public sealed partial class IconSmoothSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery;
    [Dependency] private EntityQuery<IconSmoothComponent> _iconSmoothQuery;
    [Dependency] private EntityQuery<IconSmoothGridComponent> _iconSmoothGridQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    // If there ever exists more than 256 compass directions I will kill someone.
    public static byte Directions = (byte)DirectionExtensions.AllDirections.Length;

    // Cannot access Chunk size in content even as read :P
    private static ushort ChunkSize => MapGridComponent.DefaultChunkSize;

    private readonly Queue<Entity<IconSmoothComponent>> _dirtyEntities = new();

    // Storage for similar key hashsets which exist. Entries form a free linked list when not occupied by a set of real values.
    [ViewVariables]
    private ValueList<KeyCache> _keyCaches;

    // Allocation!!!
    private HashSet<string> _workingKeyRing = new(4);
    private HashSet<string>?[] _adjacentKeys = new HashSet<string>[Directions];

    // First free position in _toleranceData.
    // -1 indicates there are no free slots left and the storage must be expanded.
    private short _freeListHead = -1;

    public override void Initialize()
    {
        base.Initialize();

        ExpandCache();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Next, update actual sprites.
        if (_dirtyEntities.Count == 0)
            return;

        // Right now performance for this isn't a huge issue.
        // If it ever does become an issue, make this a parallel job.
        // However, I'm pretty sure the caching behavior is more taxing, although that can't be made parallel without engine changes.
        while (_dirtyEntities.TryDequeue(out var entity))
        {
            CalculateNewSprite(entity);
        }
    }

    private void CalculateNewSprite(Entity<IconSmoothComponent> entity)
    {
        // Don't update our state if we can't :(
        if (!_spriteQuery.TryComp(entity, out var sprite))
            return;

        // If this entity is not eligible for IconSmooth, or the grid stores no IconSmooth data for us to use, then skip populating the array.
        var xform = Transform(entity);
        if (xform.GridUid is not { } grid
            || !xform.Anchored
            || !entity.Comp.Enabled
            || !_mapGridQuery.TryComp(grid, out var mapGrid)
            || !EnsureComp<IconSmoothGridComponent>(grid, out var iconGrid))
        {
            Array.Clear(_adjacentKeys);
            ApplyStates((entity, entity.Comp, sprite));
            return;
        }

        var tile = _map.TileIndicesFor(grid, mapGrid, xform.Coordinates);
        PopulateAdjacentKeys((grid, iconGrid, mapGrid), xform.LocalRotation, tile);
        ApplyStates((entity, entity.Comp, sprite));
    }

    private void ApplyStates(Entity<IconSmoothComponent, SpriteComponent> entity)
    {
        foreach (var spriteSmooth in entity.Comp1.States)
        {
            ApplyState((entity, entity.Comp2), spriteSmooth);
        }
    }

    private void ApplyState<T>(Entity<SpriteComponent> entity, T smoothState) where T : ISpriteSmoothState
    {
        foreach (var (key, state) in smoothState.EnumerateStates(_adjacentKeys, entity, _sprite))
        {
            _sprite.LayerSetRsiState(entity.AsNullable(), key, state);
        }
    }

    /// <summary>
    /// Enables or disables this <see cref="IconSmoothComponent"/>
    /// </summary>
    /// <param name="entity">Entity whose IconSmooth we're changing the status of.</param>
    /// <param name="enabled">Status we are changing to.</param>
    /// <param name="update">Should we also update ourselves immediately?</param>
    [PublicAPI]
    public void SetEnabled(Entity<IconSmoothComponent?> entity, bool enabled, bool update = true)
    {
        if (!_iconSmoothQuery.Resolve(entity, ref entity.Comp) || entity.Comp.Enabled == enabled)
            return;

        entity.Comp.Enabled = enabled;
        var xform = Transform(entity);

        if (enabled)
            AddTile((entity, entity.Comp, xform), entity.Comp.Key, update);
        else
            RemoveTile((entity, entity.Comp, xform), update);
    }

    [SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<IconSmoothComponent> entity, ref AnchorStateChangedEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        UpdateTile((entity, entity.Comp, args.Transform));
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<IconSmoothComponent> entity, ref ComponentInit args)
    {
        StartupLayers(entity);
    }

    private void StartupLayers(Entity<IconSmoothComponent> entity)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
        {
            Log.Error($"Entity {ToPrettyString(entity)} did not have a {nameof(SpriteComponent)}");
            return;
        }

        foreach (var state in entity.Comp.States)
        {
            state.InitializeStates((entity, sprite), _sprite);
        }
    }

    private void UpdateNeighbors(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent> grid, Vector2i pos, bool updateSelf = true)
    {
        if (updateSelf)
            _dirtyEntities.Enqueue(entity);

        foreach (var direction in DirectionExtensions.AllDirections)
        {
            UpdateAnchored(_map.GetAnchoredEntities(grid, grid, pos + direction.ToIntVec()));
        }
    }

    private void UpdateAnchored(AnchoredEntitiesEnumerator entities)
    {
        // Instead of doing HasComp -> Enqueue -> TryGetComp, we will just enqueue all entities. Generally when
        // dealing with walls neighboring anchored entities will also be walls, and in those instances that will
        // require one less component fetch/check.
        while (entities.MoveNext(out var entity))
        {
            if (_iconSmoothQuery.TryComp(entity, out var iconSmooth) && iconSmooth.Enabled)
                _dirtyEntities.Enqueue((entity.Value, iconSmooth));
        }
    }

    private void PopulateAdjacentKeys(Entity<IconSmoothGridComponent, MapGridComponent> grid, Angle localRot, Vector2i pos)
    {
        Array.Clear(_adjacentKeys);

        var offset = AngleToOffset(localRot);
        var bounds = new Box2i(pos + Vector2i.DownLeft, pos + Vector2i.UpRight);
        var chunkEnumerator = new ChunkIndicesEnumerator(bounds, ChunkSize);
        while (chunkEnumerator.MoveNext(out var chunk))
        {
            if (!grid.Comp1.Chunks.TryGetValue(chunk.Value, out var cache))
                continue;

            var chunkOrigin = chunk.Value * ChunkSize;
            var left = Math.Max(chunkOrigin.X, bounds.Left);
            var bottom = Math.Max(chunkOrigin.Y, bounds.Bottom);
            var top = Math.Min(chunkOrigin.Y + ChunkSize - 1, bounds.Top);
            var right = Math.Min(chunkOrigin.X + ChunkSize - 1, bounds.Right);

            for (var y = bottom; y <= top; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    var gridCoords = new Vector2i(x, y);
                    var vector = gridCoords - pos;
                    if (vector == Vector2i.Zero
                        || !cache.TryGetTileCache(SharedMapSystem.GetChunkRelative(gridCoords, ChunkSize), out var index))
                        continue;

                    var i = (int)vector.AsDirection() + offset;
                    if (i > 7)
                        i -= 8;

                    _adjacentKeys[i] = _keyCaches[(int)index].Keys;
                }
            }
        }
    }

    /// <summary>
    /// Converts the input angle to an offset for our Directions.
    /// We invert the angle because Direction goes Counter-Clockwise
    /// </summary>
    private byte AngleToOffset(Angle angle)
    {
        angle *= -1;
        return (byte)angle.GetCardinalDir();
    }

    private void UpdateTile(Entity<IconSmoothComponent, TransformComponent> entity)
    {
        // Wasn't attached to a grid, no tile to update :)
        if (entity.Comp2.GridUid is not { } grid || !_mapGridQuery.TryComp(grid, out var mapGrid))
            return;

        UpdateTile(entity, (grid, mapGrid));
    }

    private void UpdateTile(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent> grid)
    {
        var pos = _map.TileIndicesFor(grid, entity.Comp2.Coordinates);

        if (entity.Comp2.Anchored)
            AddTileKey(grid, pos, entity.Comp1.Key);
        else
            RemoveTileKey(grid, entity, pos);

        UpdateNeighbors(entity, (grid, grid.Comp), pos);
    }

    private void AddTile(Entity<IconSmoothComponent, TransformComponent> entity, string key, bool update = true)
    {
        if (entity.Comp2.GridUid is { } grid)
            AddTile(entity, grid, key, update);
    }

    private void AddTile(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent?> grid, string key, bool update = true)
    {
        if (!_mapGridQuery.Resolve(grid, ref grid.Comp))
            return;

        var pos = _map.TileIndicesFor((grid, grid.Comp), entity.Comp2.Coordinates);
        AddTileKey((grid, grid.Comp), pos, key);
        UpdateNeighbors(entity, (grid, grid.Comp), pos, update);
    }

    private void AddTileKey(Entity<MapGridComponent> grid, Vector2i tile, string key)
    {
        var (chunk, relative) = (_map.GridTileToChunkIndices(grid, grid.Comp, tile), SharedMapSystem.GetChunkRelative(tile, ChunkSize));
        if (!EnsureComp<IconSmoothGridComponent>(grid, out var cacheComp))
        {
            _workingKeyRing = new HashSet<string> {key};
            AddTileCache((grid,cacheComp), (chunk, relative));
            return;
        }

        if (!TryGetCache((grid, cacheComp), (chunk, relative), out var chunkData, out var cache))
        {
            _workingKeyRing = new HashSet<string> {key};
            AddTileCache((grid,cacheComp), (chunk, relative), chunkData);
            return;
        }

        if (_keyCaches[(ushort)cache].Keys is not { } keys)
        {
            _workingKeyRing = new HashSet<string> {key};
            SetTileCache((grid,cacheComp), (chunk, relative), chunkData, cache.Value);
            Log.Error($"Cache {cache} for grid {ToPrettyString(grid)}, at {tile} chunk: {chunk}, relative {relative} did not correlate to a cached value!");
            return;
        }

        _workingKeyRing = new HashSet<string>(keys);

        // Cached keys on this tile has not changed, do not update!
        if (!_workingKeyRing.Add(key))
            return;

        SetTileCache((grid, cacheComp), (chunk, relative), chunkData);
        // Properly remove the old cache!
        DecrementRefCount(cache.Value);
    }

    private void RemoveTile(Entity<IconSmoothComponent, TransformComponent> entity, bool update = true)
    {
        if (entity.Comp2.GridUid is { } grid)
            RemoveTile(entity, grid, update);
    }

    private void RemoveTile(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent?> grid, bool update = true)
    {
        if (!_mapGridQuery.Resolve(grid, ref grid.Comp))
            return;

        var tile = _map.TileIndicesFor((grid, grid.Comp), entity.Comp2.Coordinates);

        RemoveTileKey((grid, grid.Comp), entity, tile);
        UpdateNeighbors(entity, (grid, grid.Comp), tile, update);
    }

    private void RemoveTileKey(Entity<MapGridComponent> grid, EntityUid removed, Vector2i tile)
    {
        if (!_iconSmoothGridQuery.TryComp(grid, out var cacheComp))
            return;

        var (chunk, relative) = (_map.GridTileToChunkIndices(grid, grid.Comp, tile), SharedMapSystem.GetChunkRelative(tile, ChunkSize));
        if (!TryGetCache((grid, cacheComp), (chunk, relative), out var chunkData, out var tileEntry))
        {
            /*
             * This is a warning and not an error because PVS will sometimes apply the Anchoring event twice to an entity in some circumstances.
             * This exists before we DecrementRefCount so we should be fine, if DecrementRefCount ever doesn't represent the actual count, then we'll get real test fails!
             */
            Log.Warning($"{tile} on grid {ToPrettyString(grid)} was not cached despite an entity {ToPrettyString(removed)} with {nameof(IconSmoothComponent)} existing there.");
            return;
        }

        var tileEnumerator = _map.GetAnchoredEntities(grid, grid.Comp, tile);
        _workingKeyRing = new (4);
        while (tileEnumerator.MoveNext(out var uid))
        {
            if (uid == removed || !_iconSmoothQuery.TryComp(uid, out var iconSmooth) || !iconSmooth.Enabled)
                continue;

            _workingKeyRing.Add(iconSmooth.Key);
        }

        if (_workingKeyRing.Count > 0)
        {
            // Tile has not changed, don't do anything!
            if (SetMatches(tileEntry.Value))
                return;

            DecrementRefCount(tileEntry.Value);
            SetTileCache((grid, cacheComp), (chunk, relative), chunkData, CreateCacheIndex());
            return;
        }

        DecrementRefCount(tileEntry.Value);
        RemoveTileCache((grid, cacheComp), (chunk, relative), chunkData);
    }

    private void RemoveTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, IconChunkData chunkData)
    {
        chunkData.RemoveTileCache(index.Relative);
        if (chunkData.Count == 0)
            grid.Comp.Chunks.Remove(index.Chunk);
        else
            grid.Comp.Chunks[index.Chunk] = chunkData;
    }

    private void AddTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index)
    {
        AddTileCache(grid, index, new IconChunkData());
    }

    private void AddTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, IconChunkData chunkData)
    {
        AddTileCache(grid, index, chunkData, AddOrCreateCacheIndex());
    }

    private void AddTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, IconChunkData chunkData, byte cache)
    {
        chunkData.AddTileCache(index.Relative, cache);
        grid.Comp.Chunks[index.Chunk] = chunkData;
    }

    private void SetTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, IconChunkData chunkData)
    {
        SetTileCache(grid, index, chunkData, AddOrCreateCacheIndex());
    }

    private void SetTileCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, IconChunkData chunkData, byte cache)
    {
        chunkData.SetTileCache(index.Relative, cache);
        grid.Comp.Chunks[index.Chunk] = chunkData;
    }

    private bool TryGetCache(Entity<IconSmoothGridComponent> grid, (Vector2i Chunk, Vector2i Relative) index, out IconChunkData chunkData, [NotNullWhen(true)] out byte? cache)
    {
        cache = null;
        if (!grid.Comp.Chunks.TryGetValue(index.Chunk, out chunkData))
        {
            chunkData = new IconChunkData();
            return false;
        }

        return chunkData.TryGetTileCache(index.Relative, out cache);
    }

    private bool SetMatches(int i)
    {
        return _keyCaches[i].Keys?.SetEquals(_workingKeyRing) ?? false;
    }

    private byte CreateCacheIndex()
    {
        if (_freeListHead < 0)
            ExpandCache();

        var index = _freeListHead;
        _freeListHead = _keyCaches[index].RefCount;
        _keyCaches[index] = new KeyCache(_workingKeyRing);

        return (byte)index;
    }

    /// <summary>
    /// Searches for an existing Cache in our keyIndex, and creates a new one if it does not already exist.
    /// </summary>
    /// <returns>The index of the Hashset in our cache.</returns>
    private byte AddOrCreateCacheIndex()
    {
        // Faster to iterate backwards since our cache populates top down!
        for (var i = _keyCaches.Count - 1; i >= 0; i--)
        {
            if (!SetMatches(i))
                continue;

            // Cache found, increment ref count
            _keyCaches[i].RefCount++;
            return (byte)i;
        }

        return CreateCacheIndex();
    }

    private void DecrementRefCount(byte index)
    {
        ref var cacheEntry = ref _keyCaches[index];

        DebugTools.Assert(cacheEntry.RefCount > 0, $"Cache entry ref count was not greater than zero. Cache count: {cacheEntry.RefCount}");
        cacheEntry.RefCount--;
        if (cacheEntry.RefCount > 0)
            return;

        cacheEntry.Keys = null;
        cacheEntry.RefCount = _freeListHead;
        _freeListHead = index;
    }

    private void ExpandCache()
    {
        var newCacheSize = Math.Max(16, _keyCaches.Count * 2);
        DebugTools.Assert(newCacheSize <= 256, "Number of cached keys exceeded what can be stored in a byte.");
        var curSize = _keyCaches.Count;

        _keyCaches.EnsureLength(newCacheSize);
        for (var i = curSize; i < newCacheSize; i++)
        {
            _keyCaches[i].RefCount = _freeListHead;
            _freeListHead = (byte)i;
        }
    }

    private struct KeyCache(HashSet<string> keys)
    {
        public HashSet<string>? Keys = keys;

        /// <summary>
        /// Stores the number of tiles which have this set of keys.
        /// When empty, stores a reference to the next available index in _keyCache
        /// If there is no reference available, is set to -1
        /// </summary>
        public short RefCount = 1; // Doubles as freelist chain
    }
}

// TODO: Move this to engine
[Flags]
public enum Direction8Flag : byte
{
    None = 0,
    South = 1 << 0,
    SouthEast = 1 << 1,
    East = 1 << 2,
    NorthEast = 1 << 3,
    North = 1 << 4,
    NorthWest = 1 << 5,
    West = 1 << 6,
    SouthWest = 1 << 7
}
