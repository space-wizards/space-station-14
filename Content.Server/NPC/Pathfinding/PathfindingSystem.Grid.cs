using System.Linq;
using Content.Shared.NPC;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// This is equivalent to agent radii for navmeshes. In our case it's preferable that things are cleanly
    /// divisible per tile so we'll make sure it works as a discrete number.
    /// </summary>
    public const int SubStep = 4;

    public const int ChunkSize = 4;

    /*
     * TODO: For map pathfinding just straightline for now if they're both on the same map
     * Otherwise, move towards the relevant grid and when within the expanded AABB swap to grid pathfinding.
     * We can probably just have the request return partials
     * If we can't straightline to target grid we could try doing lik collision avoidance and heading to it?
     */

    /*
     * Given we aren't strictly tile-based we use a navmesh approach.
     * Navmeshes typically start with tiles and then construct polygons from there.
     *
     * Step 1 is get point data (breadcrumbs); we get multiple points per tile that contains all of the data relevant for pathfinding.
     */

    private void InitializeGrid()
    {
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
        SubscribeLocalEvent<PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
        SubscribeLocalEvent<MoveEvent>(OnMoveEvent);
    }

    private void OnCollisionChange(ref CollisionChangeEvent ev)
    {
        var xform = Transform(ev.Body.Owner);
        RebuildChunk(xform.GridUid, xform.Coordinates);
    }

    private void OnBodyTypeChange(ref PhysicsBodyTypeChangedEvent ev)
    {
        if ((ev.Old == BodyType.Static ||
            ev.New == BodyType.Static) &&
            TryComp<TransformComponent>(ev.Entity, out var xform) &&
            xform.GridUid != null)
        {
            RebuildChunk(xform.GridUid, xform.Coordinates);
        }
    }

    private void OnMoveEvent(ref MoveEvent ev)
    {
        if (!TryComp<PhysicsComponent>(ev.Sender, out var body) ||
            body.BodyType != BodyType.Static)
        {
            return;
        }

        var oldGridUid = ev.OldPosition.GetGridUid(EntityManager);
        var gridUid = ev.NewPosition.GetGridUid(EntityManager);

        // Not on a grid at all so just ignore.
        if (oldGridUid == gridUid && oldGridUid == null)
        {
            return;
        }

        var oldChunk = GetChunk(oldGridUid, ev.OldPosition);
        var chunk = GetChunk(gridUid, ev.NewPosition);

        RebuildChunk(oldChunk, oldGridUid);

        if (oldChunk != chunk)
        {
            RebuildChunk(chunk, gridUid);
        }
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<GridPathfindingComponent>(ev.EntityUid);
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        RemComp<GridPathfindingComponent>(ev.EntityUid);
    }

    private GridPathfindingChunk? GetChunk(EntityUid? gridUid, EntityCoordinates coordinates)
    {
        if (!TryComp<GridPathfindingComponent>(gridUid, out var pather))
            return null;

        var origin = GetOrigin(coordinates, gridUid.Value);

        if (origin == null)
            return null;

        return pather.Chunks.GetOrNew(origin.Value);
    }

    private Vector2i? GetOrigin(EntityCoordinates coordinates, EntityUid gridUid)
    {
        if (!TryComp<TransformComponent>(gridUid, out var gridXform))
        {
            DebugTools.Assert(false);
            return null;
        }

        var localPos = gridXform.InvWorldMatrix.Transform(coordinates.ToMapPos(EntityManager));
        return new Vector2i((int) Math.Floor(localPos.X / ChunkSize), (int) Math.Floor(localPos.Y / ChunkSize));
    }

    private void RebuildChunk(EntityUid? gridUid, EntityCoordinates coordinates)
    {
        var chunk = GetChunk(gridUid, coordinates);

        if (!TryComp<IMapGridComponent>(gridUid, out var mapgrid))
            return;

        RebuildChunk(chunk, mapgrid.Grid);
    }

    private void RebuildChunk(GridPathfindingChunk? chunk, EntityUid? gridUid)
    {
        if (chunk == null || !TryComp<IMapGridComponent>(gridUid, out var grid))
            return;

        RebuildChunk(chunk, grid.Grid);
    }

    private void RebuildChunk(GridPathfindingChunk? chunk, IMapGrid grid)
    {
        if (chunk == null)
            return;

        chunk.Clear();
        var points = chunk.Points;
        var fixturesQuery = GetEntityQuery<FixturesComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();

        // TODO: Make this more efficient
        // For now I just want to get it working.
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                // Tile
                var tile = grid.GetTileRef(new Vector2i(x, y));
                var isSpace = tile.Tile.IsEmpty;

                var tileEntities = grid.GetAnchoredEntities(new Vector2i(x, y)).ToList();

                for (var subX = 0; subX < SubStep; subX++)
                {
                    for (var subY = 0; subY < SubStep; subY++)
                    {
                        // Subtile
                        var localPos = new Vector2(x + (float) subX / SubStep, y + (float) subY / SubStep);

                        var point = new PathfindingBreadcrumb()
                        {
                            Coordinates = localPos,
                            IsSpace = isSpace,
                        };

                        points[x + y * ChunkSize] = point;

                        if (isSpace)
                        {
                            DebugTools.Assert(tileEntities.Count == 0);
                            continue;
                        }

                        foreach (var ent in tileEntities)
                        {
                            if (!fixturesQuery.TryGetComponent(ent, out var fixtures))
                                continue;

                            //  TODO: Inefficient af
                            foreach (var (_, fixture) in fixtures.Fixtures)
                            {
                                // Don't need to re-do it.
                                if ((point.CollisionMask & fixture.CollisionMask) == fixture.CollisionMask &&
                                    (point.CollisionLayer & fixture.CollisionLayer) == fixture.CollisionLayer)
                                    continue;

                                foreach (var proxy in fixture.Proxies)
                                {
                                    if (!proxy.AABB.Contains(localPos))
                                        continue;

                                    point.CollisionLayer |= fixture.CollisionLayer;
                                    point.CollisionMask |= fixture.CollisionMask;
                                }
                            }
                        }
                    }
                }
            }
        }

        // TODO: Work out neighbor nodes.
        SendBreadcrumbs();
    }
}
