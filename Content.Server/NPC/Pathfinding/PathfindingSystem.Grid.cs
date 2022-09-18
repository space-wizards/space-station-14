using System.Linq;
using Content.Shared.Maps;
using Content.Shared.NPC;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
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

    private readonly Dictionary<EntityUid, HashSet<Vector2i>> _dirtyChunks = new();

    private const float UpdateCooldown = 0.3f;
    private float _accumulator = UpdateCooldown;

    // What relevant collision groups we track for pathfinding.
    // Stuff like chairs have collision but aren't relevant for mobs.
    public const int PathfindingCollisionMask = (int) CollisionGroup.MobMask;
    public const int PathfindingCollisionLayer = (int) CollisionGroup.MobLayer;

    private void InitializeGrid()
    {
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
        SubscribeLocalEvent<PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
        SubscribeLocalEvent<MoveEvent>(OnMoveEvent);
    }

    private void UpdateGrid()
    {
        _accumulator -= UpdateCooldown;

        if (_accumulator > 0f)
        {
            return;
        }

        _accumulator += UpdateCooldown;

        // We defer chunk updates because rebuilding a navmesh is hella costly
        foreach (var (gridUid, chunks) in _dirtyChunks)
        {
            if (Deleted(gridUid))
                continue;

            foreach (var origin in chunks)
            {
                var chunk = GetChunk(gridUid, origin);
                RebuildChunk(chunk, gridUid);
            }
        }

        _dirtyChunks.Clear();
    }

    private void OnCollisionChange(ref CollisionChangeEvent ev)
    {
        var xform = Transform(ev.Body.Owner);

        if (xform.GridUid == null)
            return;

        DirtyChunk(xform.GridUid.Value, xform.Coordinates);
    }

    private void OnBodyTypeChange(ref PhysicsBodyTypeChangedEvent ev)
    {
        if ((ev.Old == BodyType.Static ||
            ev.New == BodyType.Static) &&
            TryComp<TransformComponent>(ev.Entity, out var xform) &&
            xform.GridUid != null)
        {
            DirtyChunk(xform.GridUid.Value, xform.Coordinates);
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

        if (oldGridUid != null && gridUid != null)
        {
            // If the chunk hasn't changed then just dirty that one.
            var oldOrigin = GetOrigin(ev.OldPosition, oldGridUid.Value);
            var origin = GetOrigin(ev.NewPosition, gridUid.Value);

            if (oldOrigin == origin)
            {
                // TODO: Don't need to transform again numpty.
                DirtyChunk(oldGridUid.Value, ev.NewPosition);
                return;
            }
        }

        if (oldGridUid != null)
        {
            DirtyChunk(oldGridUid.Value, ev.OldPosition);
        }

        if (gridUid != null)
        {
            DirtyChunk(gridUid.Value, ev.NewPosition);
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

    private void DirtyChunk(EntityUid gridUid, EntityCoordinates coordinates)
    {
        var chunks = _dirtyChunks.GetOrNew(gridUid);
        // TODO: Change these args around.
        chunks.Add(GetOrigin(coordinates, gridUid));
    }

    private GridPathfindingChunk? GetChunk(EntityUid? gridUid, Vector2i origin)
    {
        if (!TryComp<GridPathfindingComponent>(gridUid, out var pather))
            return null;

        if (pather.Chunks.TryGetValue(origin, out var chunk))
            return chunk;

        chunk = new GridPathfindingChunk()
        {
            Origin = origin,
        };

        pather.Chunks[origin] = chunk;
        return chunk;
    }

    private GridPathfindingChunk? GetChunk(EntityUid? gridUid, EntityCoordinates coordinates)
    {
        if (gridUid == null)
            return null;

        var origin = GetOrigin(coordinates, gridUid.Value);
        return GetChunk(gridUid, origin);
    }

    private Vector2i GetOrigin(EntityCoordinates coordinates, EntityUid gridUid)
    {
        var gridXform = Transform(gridUid);
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
        var xformQuery = GetEntityQuery<TransformComponent>();
        var gridOrigin = chunk.Origin * ChunkSize;
        var tileEntities = new ValueList<EntityUid>();

        for (var x = 0; x < ChunkSize + ExpansionSize * 2; x++)
        {
            for (var y = 0; y < ChunkSize + ExpansionSize * 2; y++)
            {
                // Tile
                var offsetX = x - ExpansionSize;
                var offsetY = y - ExpansionSize;
                var tilePos = new Vector2i(offsetX, offsetY) + gridOrigin;

                var tile = grid.GetTileRef(tilePos);
                var flags = tile.Tile.IsEmpty ? PathfindingBreadcrumbFlag.Space : PathfindingBreadcrumbFlag.None;
                var isBorder = offsetX < 0 || offsetY < 0 || offsetX >= ChunkSize || offsetY >= ChunkSize;

                if (isBorder)
                    flags |= PathfindingBreadcrumbFlag.External;

                tileEntities.Clear();
                var anchored = grid.GetAnchoredEntitiesEnumerator(tilePos);

                while (anchored.MoveNext(out var ent))
                {
                    // Irrelevant for pathfinding
                    if (!physicsQuery.TryGetComponent(ent, out var body) ||
                        !body.CanCollide ||
                        !body.Hard ||
                        ((body.CollisionLayer & PathfindingCollisionMask) == 0x0 &&
                         (body.CollisionMask & PathfindingCollisionLayer) == 0x0))
                    {
                        continue;
                    }

                    tileEntities.Add(ent.Value);
                }

                for (var subX = 0; subX < SubStep; subX++)
                {
                    for (var subY = 0; subY < SubStep; subY++)
                    {
                        var xOffset = x * SubStep + subX;
                        var yOffset = y * SubStep + subY;

                        // Subtile
                        var localPos = new Vector2(StepOffset + gridOrigin.X + offsetX + (float) subX / SubStep, StepOffset + gridOrigin.Y + offsetY + (float) subY / SubStep);
                        var collisionMask = 0x0;
                        var collisionLayer = 0x0;

                        foreach (var ent in tileEntities)
                        {
                            if (!fixturesQuery.TryGetComponent(ent, out var fixtures))
                                continue;

                            // TODO: Inefficient af
                            foreach (var (_, fixture) in fixtures.Fixtures)
                            {
                                // Don't need to re-do it.
                                if ((collisionMask & fixture.CollisionMask) == fixture.CollisionMask &&
                                    (collisionLayer & fixture.CollisionLayer) == fixture.CollisionLayer)
                                    continue;

                                // Do an AABB check first as it's probably faster, then do an actual point check.
                                var intersects = false;

                                foreach (var proxy in fixture.Proxies)
                                {
                                    if (!proxy.AABB.Contains(localPos))
                                        continue;

                                    intersects = true;
                                }

                                if (!intersects ||
                                    !xformQuery.TryGetComponent(ent, out var xform))
                                {
                                    continue;
                                }

                                if (!_fixtures.TestPoint(fixture.Shape, new Transform(xform.LocalPosition, xform.LocalRotation), localPos))
                                {
                                    continue;
                                }

                                collisionLayer |= fixture.CollisionLayer;
                                collisionMask |= fixture.CollisionMask;
                            }
                        }

                        if ((flags & PathfindingBreadcrumbFlag.Space) != 0x0)
                        {
                            DebugTools.Assert(tileEntities.Count == 0);
                        }

                        points[xOffset, yOffset] = new PathfindingBreadcrumb()
                        {
                            Coordinates = new Vector2i(xOffset, yOffset),
                            Data = new PathfindingData(flags, collisionLayer, collisionMask),
                        };
                    }
                }
            }
        }

        // Step 2. We get 2x2 cells and work out which points aren't in a cell on all 4 cardinals
        // If so then it's a boundary point.
        const int CleanupIterations = 3;
        const int originOffset = ExpansionSize * SubStep;
        var boundaryNodes = new HashSet<PathfindingBreadcrumb>();
        const int cellLength = ChunkSize * SubStep - 1;

        var cells = new PathfindingCell[cellLength, cellLength];

        for (var it = 0; it < CleanupIterations; it++)
        {
            boundaryNodes.Clear();
            Array.Clear(cells);
            var anyCleanup = false;

            // Go through anything not outside of the chunk and work out the relevant interior nodes.
            for (var x = 0; x < cellLength; x++)
            {
                for (var y = 0; y < cellLength; y++)
                {
                    var offsetX = x + originOffset;
                    var offsetY = y + originOffset;

                    ref var originPoint = ref points[offsetX, offsetY];

                    if (originPoint.Equals(PathfindingBreadcrumb.Invalid))
                    {
                        cells[x, y] = PathfindingCell.Invalid;
                        continue;
                    }

                    var data = originPoint.Data;
                    var homogenous = true;

                    for (var i = 0; i < PathfindingCell.Length; i++)
                    {
                        for (var j = 1; j < PathfindingCell.Length; j++)
                        {
                            ref var point = ref points[offsetX + i, offsetY + j];

                            if (data.Equals(point.Data))
                                continue;

                            homogenous = false;
                            goto Cell;
                        }
                    }

                    Cell: ;

                    if (!homogenous)
                    {
                        cells[x, y] = PathfindingCell.Invalid;
                        anyCleanup = true;
                        continue;
                    }

                    cells[x, y] = new PathfindingCell(data);
                }
            }

            // If we still have more iterations to try (at least until we give up).
            if (anyCleanup && it != CleanupIterations - 1)
            {
                continue;
            }

            // Alright now we have our pathfinding cells we know if any breadcrumb occupies 4 cells then it can't be a boundary.
            for (var x = 0; x < ChunkSize * SubStep; x++)
            {
                for (var y = 0; y < ChunkSize * SubStep; y++)
                {
                    var offsetX = x + originOffset;
                    var offsetY = y + originOffset;

                    // If we're on the edge of the chunk then we're a border node.
                    if (x == 0 ||
                        y == 0 ||
                        x == (ChunkSize * SubStep) - 1 ||
                        y == (ChunkSize * SubStep) - 1)
                    {
                        continue;
                    }

                    ref var point = ref points[offsetX, offsetY];
                    var isBoundary = false;

                    for (var i = -PathfindingCell.Length / 2; i <= 0; i++)
                    {
                        for (var j = -PathfindingCell.Length / 2; j <= 0; j++)
                        {
                            if (i != 0 && j != 0)
                                continue;

                            ref var cell = ref cells[x + i, y + j];

                            if (cell.Data.Equals(point.Data))
                                continue;

                            isBoundary = true;
                            goto Cell;
                        }
                    }

                    Cell: ;

                    if (!isBoundary)
                        point.Data.Flags |= PathfindingBreadcrumbFlag.Interior;
                }
            }
        }

        SendCells(chunk, grid.GridEntityId, cells);
        SendBreadcrumbs(chunk, grid.GridEntityId);

        // Step 3. Get the boundary edges
        // At this point we have a decent point cloud for navmesh or the likes
        // In our case we'll make a navmesh out of it because we aren't strictly tile-based and we likely need
        // variable sized mobs.

        var edges = new List<List<PathfindingBreadcrumb>>();

        /*
        while (boundaryNodes.Count > 0)
        {
            var seed = boundaryNodes.First();
            var node = seed;
            DebugTools.Assert(!node.IsInterior);
            boundaryNodes.Remove(node);
            var edge = new List<PathfindingBreadcrumb>()
            {
                node,
            };

            var lastDirection = DirectionFlag.None;

            while (true)
            {
                // For consistency with physics we'll go in CCW order.
                foreach (var direction in new[]
                             { DirectionFlag.West, DirectionFlag.South, DirectionFlag.East, DirectionFlag.North })
                {
                    // Don't backtrack
                    if (lastDirection != DirectionFlag.None && direction.AsDir() == lastDirection.AsDir().GetOpposite())
                        continue;

                    var offset = direction.GetOffset();

                    // Don't consider nodes outside of bounds for edges
                    if ((node.Coordinates.X + offset.X) < (ExpansionSize * SubStep) ||
                        (node.Coordinates.X + offset.X) >= ((ChunkSize + ExpansionSize) * SubStep) ||
                        (node.Coordinates.Y + offset.Y) < (ExpansionSize * SubStep) ||
                        (node.Coordinates.Y + offset.Y) >= ((ChunkSize + ExpansionSize) * SubStep))
                    {
                        continue;
                    }

                    ref var neighbor = ref points[node.Coordinates.X + offset.X, node.Coordinates.Y + offset.Y];

                    if (neighbor.IsInterior || !neighbor.Equivalent(node))
                        continue;

                    lastDirection = direction;
                    edge.Add(neighbor);
                    boundaryNodes.Remove(neighbor);
                    node = neighbor;
                    break;
                }

                if (node.Equals(seed))
                    break;
            }

            // TODO: Need to prune collinear.

            DebugTools.Assert(edge.Count > 0);
            edges.Add(edge);
        }
        */

        SendEdges(chunk, grid.GridEntityId, edges);

        // TODO: Verts
        // - Floodfill each one to get distance to nearest boundary
        // - Check distance to nearest vert and see if it's too close
        // - Check distance to boundary and see if it's too close
        // - Promote any that are too far from an existing vert / boundaries

        // TODO: Edges
        // - Choose edge candidates up to the above maximum length. Should be able to trace it along the points.
        // - Ignore any existing boundary edges / anything collinear with a boundary edge
        // - Then, sort these edges by length and consider shortest length.
        // - Reject if they intersect second-degree neighbor edges

        // TODO: Triangles
        // - Avoid having larger one encompass smaller one
    }
}
