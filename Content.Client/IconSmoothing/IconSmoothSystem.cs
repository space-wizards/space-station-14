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

        // Performance: This could be spread over multiple updates, or made parallel.
        // TODO: IParallelRobustJob
        while (_dirtyEntities.TryDequeue(out var uid))
        {
            CalculateNewSprite(uid);
        }
    }

    private void CalculateNewSprite(EntityUid uid)
    {
        // Don't update our state if we can't :(
        if (!_iconSmoothQuery.TryComp(uid, out var iconSmooth) || !_spriteQuery.TryComp(uid, out var sprite))
            return;

        // If this entity is not eligible for IconSmooth, or the grid stores no IconSmooth data for us to use, then skip populating the array.
        var xform = Transform(uid);
        if (xform.GridUid is not { } grid
            || !xform.Anchored
            || !iconSmooth.Enabled
            || !_mapGridQuery.TryComp(grid, out var mapGrid)
            || !EnsureComp<IconSmoothGridComponent>(grid, out var iconGrid))
        {
            Array.Clear(_adjacentKeys);
            ApplyStates((uid, iconSmooth, sprite));
            return;
        }

        var tile = _map.TileIndicesFor(grid, mapGrid, xform.Coordinates);
        PopulateAdjacentKeys((grid, iconGrid), xform.LocalRotation, tile);
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
        foreach (var (key, state) in smoothState.EnumerateStates(_adjacentKeys))
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
        if (entity.Comp.Enabled)
            UpdateTile((entity, entity.Comp, args.Transform), entity.Comp.Key);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<IconSmoothComponent> entity, ref ComponentStartup args)
    {
        StartupLayers(entity);

        if (!entity.Comp.Enabled)
            return;

        // If we're not anchored, no need to update any neighboring entities
        var xform = Transform(entity);
        if (!xform.Anchored)
            return;

        AddTile((entity, entity.Comp, xform), entity.Comp.Key);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<IconSmoothComponent> entity, ref ComponentShutdown args)
    {
        var xform = Transform(entity);
        if (xform.Anchored)
            RemoveTile((entity, entity.Comp, xform));
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

    private void UpdateNeighbors(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent> grid, bool updateSelf = true)
    {
        var pos = _map.TileIndicesFor(grid, entity.Comp2.Coordinates);

        UpdateNeighbors(entity, grid, pos, updateSelf);
    }

    private void UpdateNeighbors(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent> grid, Vector2i pos, bool updateSelf = true)
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
            if (_iconSmoothQuery.TryComp(entity, out var iconSmooth) && iconSmooth.Enabled)
                _dirtyEntities.Enqueue((entity.Value, iconSmooth));
        }
    }

    private void PopulateAdjacentKeys(Entity<IconSmoothGridComponent> grid, Angle localRot, Vector2i pos)
    {
        Array.Clear(_adjacentKeys);

        var i = AngleToOffset(localRot);
        foreach (var direction in EnumerateAdjacent(pos))
        {
            if (grid.Comp.Tiles.TryGetValue(direction, out var index))
                _adjacentKeys[i] = _keyCaches[index].Keys;

            // Increment i even if we don't update AdjacentKeys...
            i++;
            if (i >= Directions) // If we would go out of bounds, don't!
                i = 0;
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

    private IEnumerable<Vector2i> EnumerateAdjacent(Vector2i pos)
    {
        foreach (var vector in EnumerateDirections())
        {
            yield return vector + pos;
        }
    }

    private IEnumerable<Vector2i> EnumerateDirections()
    {
        foreach (var direction in DirectionExtensions.AllDirections)
        {
            yield return direction.ToIntVec();
        }
    }

    private void UpdateTile(Entity<IconSmoothComponent, TransformComponent> entity, string key)
    {
        // Wasn't attached to a grid, no tile to update :)
        if (entity.Comp2.GridUid is not { } grid || !_mapGridQuery.TryComp(grid, out var mapGrid))
            return;

        UpdateTile(entity, (grid, mapGrid), key);
    }

    private void UpdateTile(Entity<IconSmoothComponent, TransformComponent> entity, Entity<MapGridComponent> grid, string key)
    {
        var pos = _map.TileIndicesFor(grid, entity.Comp2.Coordinates);

        if (entity.Comp2.Anchored)
            AddTileKey(grid, pos, key);
        else
            RemoveTileKey(grid, entity, pos);

        UpdateNeighbors(entity, (grid, grid.Comp));
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

        AddTileKey((grid, grid.Comp), _map.TileIndicesFor((grid, grid.Comp), entity.Comp2.Coordinates), key);
        UpdateNeighbors(entity, (grid, grid.Comp), update);
    }

    private void AddTileKey(Entity<MapGridComponent> grid, Vector2i tile, string key)
    {
        if (!EnsureComp<IconSmoothGridComponent>(grid, out var cacheComp)
            || !cacheComp.Tiles.TryGetValue(tile, out var tileEntry))
        {
            _workingKeyRing = new HashSet<string> {key};
            cacheComp.Tiles[tile] = AddOrCreateCacheIndex();
            return;
        }

        _workingKeyRing = _keyCaches[tileEntry].Keys ?? new (2);

        // New key added, get an appropriate index for the new key!
        if (_workingKeyRing.Add(key))
        {
            tileEntry = AddOrCreateCacheIndex();
            cacheComp.Tiles[tile] = tileEntry;
            return;
        }

        _keyCaches[tileEntry].RefCount++;
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
        UpdateNeighbors(entity, (grid, grid.Comp), update);
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
        _workingKeyRing = new (4);
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

        cacheComp.Tiles[tile] = AddOrCreateCacheIndex();
    }

    /// <summary>
    /// Searches for an existing Cache in our keyIndex, and creates a new one if it does not already exist.
    /// </summary>
    /// <returns>The index of the Hashset in our cache.</returns>
    private byte AddOrCreateCacheIndex()
    {
        for (byte i = 0; i < _keyCaches.Count; i++)
        {
            if (!_keyCaches[i].Keys?.SetEquals(_workingKeyRing) ?? true)
                continue;

            // Cache found, increment ref count
            _keyCaches[i].RefCount++;
            return i;
        }

        if (_freeListHead < 0)
            ExpandCache();

        var index = _freeListHead;
        _freeListHead = _keyCaches[index].RefCount;
        _keyCaches[index] = new KeyCache(_workingKeyRing);

        return (byte)index;
    }

    private void DecrementRefCount(byte index)
    {
        ref var cacheEntry = ref _keyCaches[index];

        DebugTools.Assert(cacheEntry.RefCount > 0);
        cacheEntry.RefCount -= 1;
        if (cacheEntry.RefCount > 0)
            return;

        cacheEntry.Keys = [];
        cacheEntry.RefCount = _freeListHead;
        _freeListHead = index;
    }

    private void ExpandCache()
    {
        var newCacheSize = Math.Max(8, _keyCaches.Count * 2);
        DebugTools.Assert(newCacheSize <= 256, $"Number of cached keys exceeded what can be stored in a byte.");
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
        /// Stores a reference to the next available index in _keyCache
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
