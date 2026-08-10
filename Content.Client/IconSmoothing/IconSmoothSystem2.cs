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
public sealed partial class IconSmoothSystem2 : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery;
    [Dependency] private EntityQuery<IconSmoothComponent> _iconSmoothQuery;
    [Dependency] private EntityQuery<IconSmoothGridComponent> _iconSmoothGridQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    private readonly Queue<EntityUid> _dirtyEntities = new();

    private Dictionary<HashSet<string>, int> _keyIndex = new();

    // Storage for similar key hashsets which exist. Entries form a free linked list when not occupied by a set of real values.
    private ValueList<KeyCache> _keyCaches;

    // Allocation!!!
    private HashSet<string> _workingKeyRing = new(4);
    private ValueList<HashSet<string>> _adjacentKeys = new(8);

    // First free position in _toleranceData.
    // -1 indicates there are no free slots left and the storage must be expanded.
    private int _freeListHead = -1;

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

        // Performance: This could be spread over multiple updates, or made parallel.
        // TODO: IParallelRobustJob
        while (_dirtyEntities.TryDequeue(out var uid))
        {
            CalculateNewSprite(uid);
        }
    }

    private void CalculateNewSprite(EntityUid uid)
    {
        if (!_iconSmoothQuery.TryComp(uid, out var iconSmooth) || !_spriteQuery.TryComp(uid, out var sprite))
            return;

        // If this entity is not eligible for IconSmooth, or the grid stores no IconSmooth data for us to use, then skip populating the array.
        var xform = Transform(uid);
        if (xform.GridUid is not { } grid
            || !xform.Anchored
            || !_mapGridQuery.TryComp(grid, out var mapGrid)
            || !EnsureComp<IconSmoothGridComponent>(grid, out var iconGrid))
        {
            _adjacentKeys.Clear();
            ApplyStates((uid, iconSmooth, sprite));
            return;
        }

        var tile = _map.TileIndicesFor(grid, mapGrid, xform.Coordinates);
        PopulateAdjacentKeys((grid, iconGrid), tile);
        ApplyStates((uid, iconSmooth, sprite));
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
        foreach (var (key, state) in smoothState.EnumerateStates<Enum>(_adjacentKeys))
        {
            _sprite.LayerSetRsiState(entity.AsNullable(), key, state);
        }
    }

    /// <summary>
    /// Enables or disables this <see cref="IconSmoothComponent"/>
    /// </summary>
    /// <param name="entity">Entity whose IconSmooth we're changing the status of.</param>
    /// <param name="enabled">Status we are changing to.</param>
    [PublicAPI]
    public void SetEnabled(Entity<IconSmoothComponent?> entity, bool enabled)
    {
        if (!_iconSmoothQuery.Resolve(entity, ref entity.Comp) || entity.Comp.Enabled == enabled)
            return;

        entity.Comp.Enabled = enabled;
        var xform = Transform(entity);
        RemoveTile((entity, xform));
    }

    //[SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<IconSmoothComponent> entity, ref AnchorStateChangedEvent args)
    {
        UpdateTile((entity, args.Transform), entity.Comp.Key);
    }

    //[SubscribeLocalEvent]
    private void OnStartup(Entity<IconSmoothComponent> entity, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
        {
            Log.Error($"Entity {ToPrettyString(entity)} did not have a {nameof(SpriteComponent)}");
            return;
        }

        // TODO: Create layers on the sprite
        // TODO: Apply Shaders on the sprite
        for (int i = 0; i < entity.Comp.States.Length; i++)
        {
            var state = entity.Comp.States[i];
            state.InitializeStates((entity, sprite), _sprite);
        }

        // If we're not anchored, no need to update any neighboring entities
        var xform = Transform(entity);
        if (!xform.Anchored)
            return;

        AddTile((entity, xform), entity.Comp.Key);
    }

    //[SubscribeLocalEvent]
    private void OnShutdown(Entity<IconSmoothComponent> entity, ref ComponentShutdown args)
    {
        var xform = Transform(entity);
        RemoveTile((entity, xform));
        // TODO: Clear our states :V
    }

    private void StartupLayers()
    {

    }

    private void ShutdownLayers()
    {

    }

    private void UpdateNeighbors(Entity<TransformComponent> entity, Entity<MapGridComponent> grid, bool updateSelf = true)
    {
        var pos = _map.TileIndicesFor(grid, entity.Comp.Coordinates);

        UpdateNeighbors(entity, grid, pos, updateSelf);
    }

    private void UpdateNeighbors(Entity<TransformComponent> entity, Entity<MapGridComponent> grid, Vector2i pos, bool updateSelf = true)
    {
        if (updateSelf)
            _dirtyEntities.Enqueue(entity);

        foreach (var direction in EnumerateDirections())
        {
            UpdateAnchored(_map.GetAnchoredEntities(grid, grid, pos + direction));
        }
    }

    private void UpdateAnchored(AnchoredEntitiesEnumerator entities)
    {
        // Instead of doing HasComp -> Enqueue -> TryGetComp, we will just enqueue all entities. Generally when
        // dealing with walls neighboring anchored entities will also be walls, and in those instances that will
        // require one less component fetch/check.
        while (entities.MoveNext(out var entity))
        {
            _dirtyEntities.Enqueue(entity.Value);
        }
    }

    private void PopulateAdjacentKeys(Entity<IconSmoothGridComponent> grid, Vector2i pos)
    {
        _adjacentKeys.Clear();
        // TODO: OFFSET!!!
        var i = 0;
        foreach (var direction in EnumerateAdjacent(pos))
        {
            if (grid.Comp.Tiles.TryGetValue(direction, out var index))
                _adjacentKeys[i++] = _keyCaches[index].Keys;
        }
    }

    // TODO: Needs more testing!!!
    private sbyte AngleToOffset(Angle angle)
    {
        // TODO: Just use a switch statement tbqh...
        return (sbyte)(4 * angle / Double.Pi);
    }

    // TODO: Offsets!!!
    private IEnumerable<Vector2i> EnumerateAdjacent(Vector2i pos)
    {
        foreach (var vector in EnumerateDirections())
        {
            yield return vector + pos;
        }
    }

    private IEnumerable<Vector2i> EnumerateDirections()
    {
        foreach (var direction in Enum.GetValues<Direction>())
        {
            yield return direction.ToIntVec();
        }
    }

    private void UpdateTile(Entity<TransformComponent> entity, string key)
    {
        // Wasn't attached to a grid, no tile to update :)
        if (entity.Comp.GridUid is not { } grid || !_mapGridQuery.TryComp(grid, out var mapGrid))
            return;

        UpdateTile(entity, (grid, mapGrid), key);
    }

    private void UpdateTile(Entity<TransformComponent> entity, Entity<MapGridComponent> grid, string key)
    {
        var pos = _map.TileIndicesFor(grid, entity.Comp.Coordinates);

        if (entity.Comp.Anchored)
            AddTileKey(grid, pos, key);
        else
            RemoveTileKey(grid, entity, pos);

        UpdateNeighbors(entity, (grid, grid.Comp));
    }

    private void AddTile(Entity<TransformComponent> entity, string key)
    {
        if (entity.Comp.GridUid is { } grid)
            AddTile(entity, grid, key);
    }

    private void AddTile(Entity<TransformComponent> entity, Entity<MapGridComponent?> grid, string key)
    {
        if (!_mapGridQuery.Resolve(grid, ref grid.Comp))
            return;

        AddTileKey((grid, grid.Comp), _map.TileIndicesFor((grid, grid.Comp), entity.Comp.Coordinates), key);
        UpdateNeighbors(entity, (grid, grid.Comp));
    }

    private void AddTileKey(Entity<MapGridComponent> grid, Vector2i tile, string key)
    {
        _workingKeyRing.Clear();
        if (!EnsureComp<IconSmoothGridComponent>(grid, out var cacheComp)
            || !cacheComp.Tiles.TryGetValue(tile, out var tileEntry))
        {
            _workingKeyRing.Add(key);
            cacheComp.Tiles[tile] = GetCacheIndex(_workingKeyRing);
            return;
        }

        _workingKeyRing = _keyCaches[tileEntry].Keys;

        // Entity has same key as already existing entity on this tile, no need to update element
        if (!_workingKeyRing.Add(key))
            _keyCaches[tileEntry].RefCount++;
        else // Need to update the cache appropriately.
            cacheComp.Tiles[tile] = GetCacheIndex(_workingKeyRing);
    }

    private void RemoveTile(Entity<TransformComponent> entity)
    {
        if (entity.Comp.GridUid is { } grid)
            RemoveTile(entity, grid);
    }

    private void RemoveTile(Entity<TransformComponent> entity, Entity<MapGridComponent?> grid)
    {
        if (!_mapGridQuery.Resolve(grid, ref grid.Comp))
            return;

        var tile = _map.TileIndicesFor((grid, grid.Comp), entity.Comp.Coordinates);

        RemoveTileKey((grid, grid.Comp), entity, tile);
        UpdateNeighbors(entity, (grid, grid.Comp));
    }

    private void RemoveTileKey(Entity<MapGridComponent> grid, EntityUid removed, Vector2i tile)
    {
        if (!_iconSmoothGridQuery.TryComp(grid, out var cacheComp))
            return;

        if (!cacheComp.Tiles.TryGetValue(tile, out var tileEntry))
        {
            Log.Error($"{tile} on grid {ToPrettyString(grid)} was not cached despite an entity with {nameof(IconSmoothComponent)} existing there.");
            return;
        }

        DecrementRefCount(tileEntry);
        var tileEnumerator = _map.GetAnchoredEntities(grid, grid.Comp, tile);
        _workingKeyRing.Clear();
        while (tileEnumerator.MoveNext(out var uid))
        {
            if (uid == removed || !_iconSmoothQuery.TryComp(uid, out var iconSmooth) || !iconSmooth.Enabled)
                continue;

            _workingKeyRing.Add(iconSmooth.Key);
        }

        if (_workingKeyRing.Count == 0)
        {
            cacheComp.Tiles.Remove(tile);
            return;
        }

        cacheComp.Tiles[tile] = GetCacheIndex(_workingKeyRing);
    }

    /// <summary>
    /// Searches for an existing Cache in our keyIndex, and creates a new one if it does not already exist.
    /// </summary>
    /// <param name="keys">Hashset of keys we are searching for in our cache</param>
    /// <returns>The index of the Hashset in our cache.</returns>
    private int GetCacheIndex(HashSet<string> keys)
    {
        // TODO: Profile, array may be faster if we can't get a ref.
        if (_keyIndex.TryGetValue(keys, out var index))
            return index;

        index = _freeListHead;
        _freeListHead = _keyCaches[index].RefCount;
        _keyCaches[index] = new KeyCache(keys);
        _keyIndex[keys] = index;

        return index;
    }

    private void DecrementRefCount(int index)
    {
        ref var cacheEntry = ref _keyCaches[index];

        DebugTools.Assert(cacheEntry.RefCount > 0);
        cacheEntry.RefCount -= 1;
        if (cacheEntry.RefCount > 0)
            return;

        var prevValue = cacheEntry;
        cacheEntry.Keys = [];
        cacheEntry.RefCount = _freeListHead;
        _freeListHead = index;

        // ReSharper disable once RedundantAssignment
        var result = _keyIndex.Remove(prevValue.Keys);
        DebugTools.Assert(result, "Failed to removed 0 refcounted index!");
    }

    private void ExpandCache()
    {
        var newCacheSize = Math.Max(8, _keyCaches.Count * 2);
        var curSize = _keyCaches.Count;

        _keyCaches.EnsureLength(newCacheSize);
        for (var i = curSize; i < newCacheSize; i++)
        {
            _keyCaches[i].RefCount = _freeListHead;
            _freeListHead = i;
        }
    }

    private struct KeyCache(HashSet<string> keys)
    {
        public HashSet<string> Keys = keys;

        /// <summary>
        /// Stores a reference to the next available index in _keyCache
        /// If there is no reference available, is set to -1
        /// </summary>
        public int RefCount = 1; // Doubles as freelist chain
    }
}
