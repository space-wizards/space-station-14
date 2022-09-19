using System.Linq;
using System.Threading.Tasks;
using Content.Server.Doors.Components;
using Content.Shared.NPC;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    private static readonly TimeSpan UpdateCooldown = TimeSpan.FromSeconds(0.45);

    // What relevant collision groups we track for pathfinding.
    // Stuff like chairs have collision but aren't relevant for mobs.
    public const int PathfindingCollisionMask = (int) CollisionGroup.MobMask;
    public const int PathfindingCollisionLayer = (int) CollisionGroup.MobLayer;

    private readonly Stopwatch _stopwatch = new();

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
        var curTime = _timing.CurTime;
        var updateCount = 0;
        _stopwatch.Restart();

        // We defer chunk updates because rebuilding a navmesh is hella costly
        // If we're paused then NPCs can't run anyway.
        foreach (var comp in EntityQuery<GridPathfindingComponent>())
        {
            if (comp.DirtyChunks.Count == 0 ||
                comp.NextUpdate < curTime ||
                !TryComp<IMapGridComponent>(comp.Owner, out var mapGridComp))
            {
                continue;
            }

            var dirt = new GridPathfindingChunk[comp.DirtyChunks.Count];
            var i = 0;

            foreach (var origin in comp.DirtyChunks)
            {
                var chunk = GetChunk(origin, comp);
                dirt[i] = chunk;
                i++;
            }

            var division = 4;

            Parallel.For(0, dirt.Length / 4 + 1, i =>
            {
                // Doing the queries per task seems faster.
                var fixturesQuery = GetEntityQuery<FixturesComponent>();
                var physicsQuery = GetEntityQuery<PhysicsComponent>();
                var xformQuery = GetEntityQuery<TransformComponent>();
                BuildBreadcrumbs(dirt[i], mapGridComp.Grid, comp, fixturesQuery, physicsQuery, xformQuery);
            });

            // TODO: You could for sure do this multi-threaded in 4 iterations but I'm too lazy to do it now (ensuring no 2 neighbor chunks
            // are being operated in the same iteration). You essentially do bottom left, bottom right, top left, top right in quadrants.
            // For each 4x4 block of chunks.

            // i.e. first iteration: 0,0; 2,0; 0,2
            // second iteration: 1,0; 3,0; 1;2
            // third iteration: 0,1; 2,1; 0,3
            foreach (var chunk in dirt)
            {
                BuildNavmesh(chunk, comp);
                updateCount++;
            }

            comp.DirtyChunks.Clear();
        }

        if (updateCount > 0)
            _sawmill.Debug($"Updated {updateCount} nav chunks in {_stopwatch.Elapsed.TotalMilliseconds:0.000}ms");
    }

    private void OnCollisionChange(ref CollisionChangeEvent ev)
    {
        if (ev.Body.BodyType != BodyType.Static)
            return;

        var xform = Transform(ev.Body.Owner);

        if (xform.GridUid == null)
            return;

        // Don't re-build the navmesh on airlock changes.
        if (ev.Body.LifeStage >= ComponentLifeStage.Initialized && HasComp<AirlockComponent>(ev.Body.Owner))
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
        if (!TryComp<GridPathfindingComponent>(gridUid, out var comp))
            return;

        var currentTime = _timing.CurTime;

        if (comp.NextUpdate < currentTime)
            comp.NextUpdate = currentTime + UpdateCooldown;

        var chunks = comp.DirtyChunks;
        // TODO: Change these args around.
        chunks.Add(GetOrigin(coordinates, gridUid));
    }

    private GridPathfindingChunk GetChunk(Vector2i origin, GridPathfindingComponent component)
    {
        if (component.Chunks.TryGetValue(origin, out var chunk))
            return chunk;

        chunk = new GridPathfindingChunk()
        {
            Origin = origin,
        };

        component.Chunks[origin] = chunk;
        return chunk;
    }

    private Vector2i GetOrigin(EntityCoordinates coordinates, EntityUid gridUid)
    {
        var gridXform = Transform(gridUid);
        var localPos = gridXform.InvWorldMatrix.Transform(coordinates.ToMapPos(EntityManager));
        return new Vector2i((int) Math.Floor(localPos.X / ChunkSize), (int) Math.Floor(localPos.Y / ChunkSize));
    }

    private void BuildBreadcrumbs(GridPathfindingChunk chunk,
        IMapGrid grid,
        GridPathfindingComponent component,
        EntityQuery<FixturesComponent> fixturesQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var sw = new Stopwatch();
        sw.Start();
        chunk.Clear(component);
        var points = chunk.Points;
        var gridOrigin = chunk.Origin * ChunkSize;
        var tileEntities = new ValueList<EntityUid>();
        var chunkPolys = chunk.Polygons;
        var tilePolys = new ValueList<Box2i>(SubStep);

        // Need to get the relevant polygons in each tile.
        // If we wanted to create a larger navmesh we could triangulate these points but in our case we're just going
        // to treat them as tile-based.
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                // Tile
                var tilePos = new Vector2i(x, y) + gridOrigin;
                tilePolys.Clear();

                var tile = grid.GetTileRef(tilePos);
                var flags = tile.Tile.IsEmpty ? PathfindingBreadcrumbFlag.Space : PathfindingBreadcrumbFlag.None;
                var isBorder = x < 0 || y < 0 || x == ChunkSize - 1 || y == ChunkSize - 1;

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
                        var localPos = new Vector2(StepOffset + gridOrigin.X + x + (float) subX / SubStep, StepOffset + gridOrigin.Y + y + (float) subY / SubStep);
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

                        var crumb = new PathfindingBreadcrumb()
                        {
                            Coordinates = new Vector2i(xOffset, yOffset),
                            Data = new PathfindingData(flags, collisionLayer, collisionMask),
                        };

                        points[xOffset, yOffset] = crumb;
                    }
                }

                // Now we got tile data and we can get the polys
                var data = points[x * SubStep, y * SubStep].Data;
                var start = Vector2i.Zero;

                for (var i = 0; i < SubStep * SubStep; i++)
                {
                    var ix = i / SubStep;
                    var iy = i % SubStep;

                    var nextX = (i + 1) / SubStep;
                    var nextY = (i + 1) % SubStep;

                    // End point
                    if (iy == SubStep - 1 ||
                        !points[x * SubStep + nextX, y * SubStep + nextY].Data.Equals(data))
                    {
                        tilePolys.Add(new Box2i(start, new Vector2i(ix, iy)));
                        start = new Vector2i(nextX, nextY);
                        data = points[x * SubStep + nextX, y * SubStep + nextY].Data;
                        continue;
                    }
                }

                // Now combine the lines
                var anyCombined = true;

                while (anyCombined)
                {
                    anyCombined = false;

                    for (var i = 0; i < tilePolys.Count; i++)
                    {
                        var poly = tilePolys[i];
                        data = points[x * SubStep + poly.Left, y * SubStep + poly.Bottom].Data;

                        for (var j = i + 1; j < tilePolys.Count; j++)
                        {
                            var nextPoly = tilePolys[j];
                            var nextData = points[x * SubStep + nextPoly.Left, y * SubStep + nextPoly.Bottom].Data;

                            // Oh no, Combine
                            if (poly.Bottom == nextPoly.Bottom &&
                                poly.Top == nextPoly.Top &&
                                poly.Right + 1 == nextPoly.Left &&
                                data.Equals(nextData))
                            {
                                tilePolys.RemoveAt(j);
                                j--;
                                poly = new Box2i(poly.Left, poly.Bottom, poly.Right + 1, poly.Top);
                                anyCombined = true;
                            }
                        }

                        tilePolys[i] = poly;
                    }
                }

                var tilePoly = chunkPolys[x, y];
                tilePoly.Clear();
                var polyOffset = gridOrigin + new Vector2(x, y);

                foreach (var poly in tilePolys)
                {
                    var box = new Box2((Vector2) poly.BottomLeft / SubStep + polyOffset,
                        (Vector2) (poly.TopRight + Vector2i.One) / SubStep + polyOffset);
                    var polyData = points[x * SubStep + poly.Left, y * SubStep + poly.Bottom].Data;

                    tilePoly.Add(new PathPoly(box, polyData));
                }

                chunkPolys[x, y] = tilePoly;
            }
        }

        _sawmill.Debug($"Built breadcrumbs in {sw.Elapsed.TotalMilliseconds}ms");
        SendBreadcrumbs(chunk, grid.GridEntityId);
    }

    private void BuildNavmesh(GridPathfindingChunk chunk, GridPathfindingComponent component)
    {
        var sw = new Stopwatch();
        sw.Start();
        var chunkPolys = chunk.Polygons;
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(-1, 0), out var leftChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(0, -1), out var bottomChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(1, 0), out var rightChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(0, 1), out var topChunk);

        // Now we can get the neighbors for our tile polys
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var tile = chunkPolys[x, y];
                var index = (byte) (x * ChunkSize + y);

                for (byte i = 0; i < tile.Count; i++)
                {
                    var poly = tile[i];

                    var polyRef = new PathPolyRef()
                    {
                        ChunkOrigin = chunk.Origin,
                        Index = index,
                        TileIndex = i,
                    };

                    var enlarged = poly.Box.Enlarged(StepOffset);

                    // Shouldn't need to wraparound as previous neighbors would've handled us.
                    for (var j = (byte) (i + 1); j < tile.Count; j++)
                    {
                        var neighbor = tile[j];
                        var enlargedNeighbor = neighbor.Box.Enlarged(StepOffset);
                        var overlap = Box2.Area(enlarged.Intersect(enlargedNeighbor));

                        // Need to ensure they intersect by at least 2 tiles.
                        if (overlap <= 0.5f / SubStep)
                            continue;

                        var neighborRef = new PathPolyRef()
                        {
                            ChunkOrigin = chunk.Origin,
                            Index = index,
                            TileIndex = j,
                        };

                        poly.Neighbors.Add(neighborRef);
                        neighbor.Neighbors.Add(polyRef);
                    }

                    // TODO: Get neighbor tile polys
                    for (var ix = -1; ix <= 1; ix++)
                    {
                        for (var iy = -1; iy <= 1; iy++)
                        {
                            if (ix != 0 && iy != 0)
                                continue;

                            var neighborX = x + ix;
                            var neighborY = y + iy;
                            List<PathPoly> neighborTile;
                            GridPathfindingChunk neighborChunk;

                            if (neighborX < 0)
                            {
                                if (leftChunk == null)
                                    continue;

                                neighborX = ChunkSize - 1;
                                neighborTile = leftChunk.Polygons[neighborX, neighborY];
                                neighborChunk = leftChunk;
                            }
                            else if (neighborY < 0)
                            {
                                if (bottomChunk == null)
                                    continue;

                                neighborY = ChunkSize - 1;
                                neighborTile = bottomChunk.Polygons[neighborX, neighborY];
                                neighborChunk = bottomChunk;
                            }
                            else if (neighborX >= ChunkSize)
                            {
                                if (rightChunk == null)
                                    continue;

                                neighborX = 0;
                                neighborTile = rightChunk.Polygons[neighborX, neighborY];
                                neighborChunk = rightChunk;
                            }
                            else if (neighborY >= ChunkSize)
                            {
                                if (topChunk == null)
                                    continue;

                                neighborY = 0;
                                neighborTile = topChunk.Polygons[neighborX, neighborY];
                                neighborChunk = topChunk;
                            }
                            else
                            {
                                neighborTile = chunkPolys[neighborX, neighborY];
                                neighborChunk = chunk;
                            }

                            var neighborIndex = (byte) (neighborX * ChunkSize + neighborY);

                            for (byte j = 0; j < neighborTile.Count; j++)
                            {
                                var neighbor = neighborTile[j];
                                var enlargedNeighbor = neighbor.Box.Enlarged(StepOffset);
                                var overlap = Box2.Area(enlarged.Intersect(enlargedNeighbor));

                                // Need to ensure they intersect by at least 2 tiles.
                                if (overlap <= 0.5f / SubStep)
                                    continue;

                                var neighborRef = new PathPolyRef()
                                {
                                    ChunkOrigin = neighborChunk.Origin,
                                    Index = neighborIndex,
                                    TileIndex = j,
                                };

                                poly.Neighbors.Add(neighborRef);
                                neighbor.Neighbors.Add(polyRef);
                            }
                        }
                    }
                }
            }
        }

        _sawmill.Debug($"Built navmesh in {sw.Elapsed.TotalMilliseconds}ms");
        SendTilePolys(chunk, component.Owner, chunkPolys);
    }
}
