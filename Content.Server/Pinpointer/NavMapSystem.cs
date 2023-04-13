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
                RefreshTile(oldGrid, navMap, chunk, ev.TilePos);
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

        if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
        {
            chunk = new NavMapChunk(chunkOrigin);
            navMap.Chunks[chunkOrigin] = chunk;
        }

        RefreshTile(grid, navMap, chunk, tile);
    }

    private void RefreshTile(MapGridComponent grid, NavMapComponent component, NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);

        var existing = chunk.TileData;
        var flag = GetFlag(relative);

        chunk.TileData &= ~flag;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(tile);
        // TODO: Use something to get convex poly.
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();

        while (enumerator.MoveNext(out var ent))
        {
            if (!bodyQuery.TryGetComponent(ent, out var body) ||
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
