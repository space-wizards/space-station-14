using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Pinpointer;

/// <summary>
/// Handles data to be used for in-grid map displays.
/// </summary>
public sealed class NavMapSystem : SharedNavMapSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    // TODO: Chuck it in shared IG with diffs IG? Seems the least bandwidth intensive overall.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<NavMapComponent, ComponentGetState>(OnGetState);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<NavMapComponent>(ev.EntityUid);
    }

    private void OnGetState(EntityUid uid, NavMapComponent component, ref ComponentGetState args)
    {
        var data = new Dictionary<Vector2i, int>(component.Chunks.Count);

        foreach (var (index, chunk) in component.Chunks)
        {
            data.Add(index, chunk.TileData);
        }

        // TODO: Dear lord this will need diffs.
        args.State = new NavMapComponentState()
        {
            TileData = data,
        };
    }

    private void OnAnchorChange(ref AnchorStateChangedEvent ev)
    {
        if (!TryComp<NavMapComponent>(ev.Transform.GridUid, out var navMap) ||
            !TryComp<MapGridComponent>(ev.Transform.GridUid, out var grid))
            return;

        var tile = grid.LocalToTile(ev.Transform.Coordinates);
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

        // TODO: Diff existing data.
        var flag = GetFlag(relative);

        chunk.TileData &= ~flag;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(tile);
        // TODO: Use something to get convex poly.
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var fixturesQuery = GetEntityQuery<FixturesComponent>();

        while (enumerator.MoveNext(out var ent))
        {
            if (!bodyQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard ||
                body.BodyType != BodyType.Static ||
                !fixturesQuery.TryGetComponent(ent, out var manager))
            {
                continue;
            }

            foreach (var fixture in manager.Fixtures.Values)
            {
                if (fixture.Shape is not PolygonShape poly)
                    continue;

                var data = new Vector2[poly.VertexCount];

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    data[i] = poly.Vertices[i] + tile * grid.TileSize;
                }

                chunk.TileData |= flag;
                break;
            }

            break;
        }

        Dirty(component);

        if (chunk.TileData == 0)
        {
            component.Chunks.Remove(chunk.Origin);
            return;
        }

        chunk.LastUpdate = _timing.CurTick;
    }
}
