using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Pinpointer;

/// <summary>
/// Handles data to be used for in-grid map displays.
/// </summary>
public sealed class NavMapSystem : SharedNavMapSystem
{
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NavMapComponent, GridSplitEvent>(OnNavMapSplit);
        SubscribeLocalEvent<StationGridAddedEvent>(OnStationInit);
    }

    private void OnStationInit(StationGridAddedEvent ev)
    {
        var comp = EnsureComp<NavMapComponent>(ev.GridId);
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();
        RefreshGrid(comp, Comp<MapGridComponent>(ev.GridId), physicsQuery, tagQuery);
    }

    private void OnNavMapSplit(EntityUid uid, NavMapComponent component, ref GridSplitEvent args)
    {
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();
        var gridQuery = GetEntityQuery<MapGridComponent>();

        foreach (var grid in args.NewGrids)
        {
            var newComp = EnsureComp<NavMapComponent>(grid);
            RefreshGrid(newComp, gridQuery.GetComponent(grid), physicsQuery, tagQuery);
        }

        RefreshGrid(component, gridQuery.GetComponent(uid), physicsQuery, tagQuery);
    }

    private void RefreshGrid(NavMapComponent component, MapGridComponent grid, EntityQuery<PhysicsComponent> physicsQuery, EntityQuery<TagComponent> tagQuery)
    {
        component.Chunks.Clear();

        var tiles = grid.GetAllTilesEnumerator();

        while (tiles.MoveNext(out var tile))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.Value.GridIndices, ChunkSize);

            if (!component.Chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunk(chunkOrigin);
                component.Chunks[chunkOrigin] = chunk;
            }

            RefreshTile(grid, component, chunk, tile.Value.GridIndices, physicsQuery, tagQuery);
        }
    }

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        var data = new Dictionary<Vector2i, int>(component.Chunks.Count);
        foreach (var (index, chunk) in component.Chunks)
        {
            data.Add(index, chunk.TileData);
        }

        // TODO: Diffs
        args.State = new NavMapComponentState()
        {
            TileData = data,
        };
    }

    private void OnReAnchor(ref ReAnchorEvent ev)
    {
        if (TryComp<MapGridComponent>(ev.OldGrid, out var oldGrid) &&
            TryComp<NavMapComponent>(ev.OldGrid, out var navMap))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(ev.TilePos, ChunkSize);

            if (navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                var physicsQuery = GetEntityQuery<PhysicsComponent>();
                var tagQuery = GetEntityQuery<TagComponent>();
                RefreshTile(oldGrid, navMap, chunk, ev.TilePos, physicsQuery, tagQuery);
            }
        }

        HandleAnchor(ev.Xform);
    }

    private void OnAnchorChange(ref AnchorStateChangedEvent ev)
    {
        HandleAnchor(ev.Transform);
    }

    private void HandleAnchor(TransformComponent xform)
    {
        if (!TryComp<NavMapComponent>(xform.GridUid, out var navMap) ||
            !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = grid.LocalToTile(xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, ChunkSize);
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();

        if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
        {
            chunk = new NavMapChunk(chunkOrigin);
            navMap.Chunks[chunkOrigin] = chunk;
        }

        RefreshTile(grid, navMap, chunk, tile, physicsQuery, tagQuery);
    }

    private void RefreshTile(MapGridComponent grid, NavMapComponent component, NavMapChunk chunk, Vector2i tile,
        EntityQuery<PhysicsComponent> physicsQuery,
        EntityQuery<TagComponent> tagQuery)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);

        var existing = chunk.TileData;
        var flag = GetFlag(relative);

        chunk.TileData &= ~flag;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(tile);
        // TODO: Use something to get convex poly.

        while (enumerator.MoveNext(out var ent))
        {
            if (!physicsQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard ||
                body.BodyType != BodyType.Static ||
                (!_tags.HasTag(ent.Value, "Wall", tagQuery) &&
                 !_tags.HasTag(ent.Value, "Window", tagQuery)))
            {
                continue;
            }

            chunk.TileData |= flag;
            break;
        }

        if (chunk.TileData == 0)
        {
            component.Chunks.Remove(chunk.Origin);
        }

        if (existing == chunk.TileData)
            return;

        Dirty(component);
    }
}
