using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Destructible;
using Content.Shared.Access.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Doors.Components;
using Content.Shared.NPC;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
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

    /// <summary>
    ///     If true, UpdateGrid() will not process grids.
    /// </summary>
    /// <remarks>
    ///     Useful if something like a large explosion is in the process of shredding the grid, as it avoids unneccesary
    ///     updating.
    /// </remarks>
    public bool PauseUpdating = false;

    private readonly Stopwatch _stopwatch = new();

    // Probably can't pool polys as there might be old pathfinding refs to them.

    private void InitializeGrid()
    {
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<GridPathfindingComponent, ComponentShutdown>(OnGridPathShutdown);
        SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChange);
        SubscribeLocalEvent<CollisionLayerChangeEvent>(OnCollisionLayerChange);
        SubscribeLocalEvent<PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChange);
        _transform.OnGlobalMoveEvent += OnMoveEvent;
    }

    private void OnTileChange(ref TileChangedEvent ev)
    {
        if (ev.OldTile.IsEmpty == ev.NewTile.Tile.IsEmpty)
            return;

        DirtyChunk(ev.Entity, Comp<MapGridComponent>(ev.Entity).GridTileToLocal(ev.NewTile.GridIndices));
    }


    private void OnGridPathShutdown(EntityUid uid, GridPathfindingComponent component, ComponentShutdown args)
    {
        foreach (var chunk in component.Chunks)
        {
            // Invalidate all polygons in case there's portals or the likes.
            foreach (var poly in chunk.Value.Polygons)
            {
                ClearTilePolys(poly);
            }
        }

        component.DirtyChunks.Clear();
        component.Chunks.Clear();
    }

    private void UpdateGrid(ParallelOptions options)
    {
        if (PauseUpdating)
            return;

        var curTime = _timing.CurTime;
#if DEBUG
        var updateCount = 0;
#endif
        _stopwatch.Restart();

        // We defer chunk updates because rebuilding a navmesh is hella costly
        // Still run even when paused.
        var query = AllEntityQuery<GridPathfindingComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var pathfinding = new Entity<GridPathfindingComponent>(uid, comp);
            // TODO: Dump all this shit and just do it live it's probably fast enough.
            if (comp.DirtyChunks.Count == 0 ||
                curTime < comp.NextUpdate ||
                !_gridQuery.TryGetComponent(uid, out var mapGridComp))
            {
                continue;
            }

            var dirtyPortals = comp.DirtyPortals;
            dirtyPortals.Clear();

            // TODO: Often we invalidate the entire chunk when it might be something as simple as an airlock change
            // Would be better to handle that though this was safer and max it's taking is like 1-2ms every half-second.
            var dirt = new GridPathfindingChunk[comp.DirtyChunks.Count];
            var idx = 0;

            foreach (var origin in comp.DirtyChunks)
            {
                var chunk = GetChunk(origin, uid, pathfinding);
                dirt[idx] = chunk;
                idx++;
            }

            // We force clear portals in a single-threaded context to be safe
            // as they may not be thread-safe to touch.
            foreach (var chunk in dirt)
            {
                foreach (var (_, poly) in chunk.PortalPolys)
                {
                    ClearPoly(poly);
                }

                chunk.PortalPolys.Clear();

                foreach (var portal in chunk.Portals)
                {
                    dirtyPortals.Add(portal);
                }
            }

            // TODO: Inflate grid bounds slightly and get chunks.
            // This is for map <> grid pathfinding

            // Without parallel this is roughly 3x slower on my desktop.
            Parallel.For(0, dirt.Length, options, i =>
            {
                BuildBreadcrumbs(dirt[i], (uid, mapGridComp));
            });

            const int Division = 4;

            // You can safely do this in parallel as long as no neighbor chunks are being touched in the same iteration.
            // You essentially do bottom left, bottom right, top left, top right in quadrants.
            // For each 4x4 block of chunks.

            // i.e. first iteration: 0,0; 2,0; 0,2
            // second iteration: 1,0; 3,0; 1;2
            // third iteration: 0,1; 2,1; 0,3 etc

            for (var it = 0; it < Division; it++)
            {
                var it1 = it;

                Parallel.For(0, dirt.Length, options, j =>
                {
                    var chunk = dirt[j];
                    // Check if the chunk is safe on this iteration.
                    var x = Math.Abs(chunk.Origin.X % 2);
                    var y = Math.Abs(chunk.Origin.Y % 2);
                    var index = x * 2 + y;

                    if (index != it1)
                        return;

                    ClearOldPolys(chunk);
                });
            }

            // TODO: You can probably skimp on some neighbor chunk caches
            for (var it = 0; it < Division; it++)
            {
                var it1 = it;

                Parallel.For(0, dirt.Length, options, j =>
                {
                    var chunk = dirt[j];
                    // Check if the chunk is safe on this iteration.
                    var x = Math.Abs(chunk.Origin.X % 2);
                    var y = Math.Abs(chunk.Origin.Y % 2);
                    var index = x * 2 + y;

                    if (index != it1)
                        return;

                    BuildNavmesh(chunk, pathfinding);
#if DEBUG
                    Interlocked.Increment(ref updateCount);
#endif
                });
            }

            // Handle portals at the end after having cleared their neighbors above.
            // We do this because there's no guarantee of where these are for chunks.
            foreach (var portal in dirtyPortals)
            {
                var polyA = GetPoly(portal.CoordinatesA);
                var polyB = GetPoly(portal.CoordinatesB);

                if (polyA == null || polyB == null)
                    continue;

                DebugTools.Assert((polyA.Data.Flags & PathfindingBreadcrumbFlag.Invalid) == 0x0);
                DebugTools.Assert((polyB.Data.Flags & PathfindingBreadcrumbFlag.Invalid) == 0x0);
                var chunkA = GetChunk(polyA.ChunkOrigin, polyA.GraphUid);
                var chunkB = GetChunk(polyB.ChunkOrigin, polyB.GraphUid);

                chunkA.PortalPolys.TryAdd(portal, polyA);
                chunkB.PortalPolys.TryAdd(portal, polyB);
                AddNeighbors(polyA, polyB);
            }

            comp.DirtyChunks.Clear();
        }
    }

    private bool IsBodyRelevant(FixturesComponent fixtures)
    {
        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            if ((fixture.CollisionMask & PathfindingCollisionLayer) != 0x0 ||
                (fixture.CollisionLayer & PathfindingCollisionMask) != 0x0)
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionChange(ref CollisionChangeEvent ev)
    {
        var xform = Transform(ev.BodyUid);

        if (xform.GridUid == null)
            return;

        // This will also rebuild on door open / closes which I think is good?
        var aabb = _lookup.GetAABBNoContainer(ev.BodyUid, xform.Coordinates.Position, xform.LocalRotation);
        DirtyChunkArea(xform.GridUid.Value, aabb);
    }

    private void OnCollisionLayerChange(ref CollisionLayerChangeEvent ev)
    {
        var xform = Transform(ev.Body);

        if (xform.GridUid == null)
            return;

        var aabb = _lookup.GetAABBNoContainer(ev.Body, xform.Coordinates.Position, xform.LocalRotation);
        DirtyChunkArea(xform.GridUid.Value, aabb);
    }

    private void OnBodyTypeChange(ref PhysicsBodyTypeChangedEvent ev)
    {
        if (TryComp<TransformComponent>(ev.Entity, out var xform) &&
            xform.GridUid != null)
        {
            var aabb = _lookup.GetAABBNoContainer(ev.Entity, xform.Coordinates.Position, xform.LocalRotation);
            DirtyChunkArea(xform.GridUid.Value, aabb);
        }
    }

    private void OnMoveEvent(ref MoveEvent ev)
    {
        if (!_fixturesQuery.TryGetComponent(ev.Sender, out var fixtures) ||
            !IsBodyRelevant(fixtures) ||
            _gridQuery.HasComponent(ev.Sender))
        {
            return;
        }

        var gridUid = ev.Component.GridUid;
        var oldGridUid = ev.OldPosition.EntityId == ev.NewPosition.EntityId
            ? gridUid
            : ev.OldPosition.GetGridUid(EntityManager);

        if (oldGridUid != null && oldGridUid != gridUid)
        {
            var aabb = _lookup.GetAABBNoContainer(ev.Sender, ev.OldPosition.Position, ev.OldRotation);
            DirtyChunkArea(oldGridUid.Value, aabb);
        }

        if (gridUid != null)
        {
            var aabb = _lookup.GetAABBNoContainer(ev.Sender, ev.NewPosition.Position, ev.NewRotation);
            DirtyChunkArea(gridUid.Value, aabb);
        }
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<GridPathfindingComponent>(ev.EntityUid);

        // Pathfinder refactor
        var mapGrid = Comp<MapGridComponent>(ev.EntityUid);

        for (var x = Math.Floor(mapGrid.LocalAABB.Left); x <= Math.Ceiling(mapGrid.LocalAABB.Right + ChunkSize); x += ChunkSize)
        {
            for (var y = Math.Floor(mapGrid.LocalAABB.Bottom); y <= Math.Ceiling(mapGrid.LocalAABB.Top + ChunkSize); y += ChunkSize)
            {
                DirtyChunk(ev.EntityUid, mapGrid.GridTileToLocal(new Vector2i((int) x, (int) y)));
            }
        }
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        RemComp<GridPathfindingComponent>(ev.EntityUid);
    }

    /// <summary>
    /// Queues the entire relevant chunk to be re-built in the next update.
    /// </summary>
    private void DirtyChunk(EntityUid gridUid, EntityCoordinates coordinates)
    {
        if (!TryComp<GridPathfindingComponent>(gridUid, out var comp))
            return;

        var currentTime = _timing.CurTime;

        if (comp.NextUpdate < currentTime && !MetaData(gridUid).EntityPaused)
            comp.NextUpdate = currentTime + UpdateCooldown;

        var chunks = comp.DirtyChunks;
        // TODO: Change these args around.
        chunks.Add(GetOrigin(coordinates, gridUid));
    }

    private void DirtyChunkArea(EntityUid gridUid, Box2 aabb)
    {
        if (!TryComp<GridPathfindingComponent>(gridUid, out var comp))
            return;

        var currentTime = _timing.CurTime;

        if (comp.NextUpdate < currentTime)
            comp.NextUpdate = currentTime + UpdateCooldown;

        var chunks = comp.DirtyChunks;

        // This assumes you never have bounds equal to or larger than 2 * ChunkSize.
        var corners = new Vector2[] { aabb.BottomLeft, aabb.TopRight, aabb.BottomRight, aabb.TopLeft };
        foreach (var corner in corners)
        {
            var sampledPoint = new Vector2i(
                (int) Math.Floor((corner.X) / ChunkSize),
                (int) Math.Floor((corner.Y) / ChunkSize));

            chunks.Add(sampledPoint);
        }
    }

    private GridPathfindingChunk GetChunk(Vector2i origin, EntityUid uid, GridPathfindingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            throw new InvalidOperationException();
        }

        if (component.Chunks.TryGetValue(origin, out var chunk))
            return chunk;

        chunk = new GridPathfindingChunk()
        {
            Origin = origin,
        };

        component.Chunks[origin] = chunk;
        return chunk;
    }

    private bool TryGetChunk(Vector2i origin, GridPathfindingComponent component, [NotNullWhen(true)] out GridPathfindingChunk? chunk)
    {
        return component.Chunks.TryGetValue(origin, out chunk);
    }

    private byte GetIndex(int x, int y)
    {
        return (byte) (x * ChunkSize + y);
    }

    private Vector2i GetOrigin(Vector2 localPos)
    {
        return new Vector2i((int) Math.Floor(localPos.X / ChunkSize), (int) Math.Floor(localPos.Y / ChunkSize));
    }

    private Vector2i GetOrigin(EntityCoordinates coordinates, EntityUid gridUid)
    {
        var localPos = _transform.GetInvWorldMatrix(gridUid).Transform(coordinates.ToMapPos(EntityManager, _transform));
        return new Vector2i((int) Math.Floor(localPos.X / ChunkSize), (int) Math.Floor(localPos.Y / ChunkSize));
    }

    private void BuildBreadcrumbs(GridPathfindingChunk chunk, Entity<MapGridComponent> grid)
    {
        var sw = new Stopwatch();
        sw.Start();
        var points = chunk.Points;
        var gridOrigin = chunk.Origin * ChunkSize;
        var tileEntities = new ValueList<EntityUid>();
        var chunkPolys = chunk.BufferPolygons;

        for (var i = 0; i < chunkPolys.Length; i++)
        {
            chunkPolys[i].Clear();
        }

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

                var tile = _maps.GetTileRef(grid.Owner, grid.Comp, tilePos);
                var flags = tile.Tile.IsEmpty ? PathfindingBreadcrumbFlag.Space : PathfindingBreadcrumbFlag.None;
                // var isBorder = x < 0 || y < 0 || x == ChunkSize - 1 || y == ChunkSize - 1;

                tileEntities.Clear();
                var available = _lookup.GetLocalEntitiesIntersecting(tile, flags: LookupFlags.Dynamic | LookupFlags.Static);

                foreach (var ent in available)
                {
                    // Irrelevant for pathfinding
                    if (!_fixturesQuery.TryGetComponent(ent, out var fixtures) ||
                        !IsBodyRelevant(fixtures))
                    {
                        continue;
                    }

                    var xform = _xformQuery.GetComponent(ent);

                    if (xform.ParentUid != grid.Owner ||
                        _maps.LocalToTile(grid.Owner, grid.Comp, xform.Coordinates) != tilePos)
                    {
                        continue;
                    }

                    tileEntities.Add(ent);
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
                        var damage = 0f;

                        foreach (var ent in tileEntities)
                        {
                            if (!_fixturesQuery.TryGetComponent(ent, out var fixtures))
                                continue;

                            var colliding = false;

                            foreach (var fixture in fixtures.Fixtures.Values)
                            {
                                // Don't need to re-do it.
                                if (!fixture.Hard ||
                                    (collisionMask & fixture.CollisionMask) == fixture.CollisionMask &&
                                    (collisionLayer & fixture.CollisionLayer) == fixture.CollisionLayer)
                                {
                                    continue;
                                }

                                // Do an AABB check first as it's probably faster, then do an actual point check.
                                var intersects = false;

                                foreach (var proxy in fixture.Proxies)
                                {
                                    if (!proxy.AABB.Contains(localPos))
                                        continue;

                                    intersects = true;
                                }

                                if (!intersects ||
                                    !_xformQuery.TryGetComponent(ent, out var xform))
                                {
                                    continue;
                                }

                                if (!_fixtures.TestPoint(fixture.Shape, new Transform(xform.LocalPosition, xform.LocalRotation), localPos))
                                {
                                    continue;
                                }

                                collisionLayer |= fixture.CollisionLayer;
                                collisionMask |= fixture.CollisionMask;
                                colliding = true;
                            }

                            // If entity doesn't intersect this node (e.g. thindows) then ignore it.
                            if (!colliding)
                                continue;

                            if (_accessQuery.HasComponent(ent))
                            {
                                flags |= PathfindingBreadcrumbFlag.Access;
                            }

                            if (_doorQuery.HasComponent(ent))
                            {
                                flags |= PathfindingBreadcrumbFlag.Door;
                            }

                            if (_climbableQuery.HasComponent(ent))
                            {
                                flags |= PathfindingBreadcrumbFlag.Climb;
                            }

                            if (_destructibleQuery.TryGetComponent(ent, out var damageable))
                            {
                                damage += _destructible.DestroyedAt(ent, damageable).Float();
                            }
                        }

                        /*This is causing too many issues and I'd rather just ignore it until pathfinder refactor
                          to just get tiles at runtime.
                        if ((flags & PathfindingBreadcrumbFlag.Space) != 0x0)
                        {
                            // DebugTools.Assert(tileEntities.Count == 0);
                        }
                        */

                        var crumb = new PathfindingBreadcrumb()
                        {
                            Coordinates = new Vector2i(xOffset, yOffset),
                            Data = new PathfindingData(flags, collisionLayer, collisionMask, damage),
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

                        if (i < (SubStep * SubStep) - 1)
                        {
                            start = new Vector2i(nextX, nextY);
                            data = points[x * SubStep + nextX, y * SubStep + nextY].Data;
                        }
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

                // TODO: Can store a hash for each tile and check if the breadcrumbs match and avoid allocating these at all.
                var tilePoly = chunkPolys[x * ChunkSize + y];
                var polyOffset = gridOrigin + new Vector2(x, y);

                foreach (var poly in tilePolys)
                {
                    var box = new Box2((Vector2) poly.BottomLeft / SubStep + polyOffset,
                        (Vector2) (poly.TopRight + Vector2i.One) / SubStep + polyOffset);
                    var polyData = points[x * SubStep + poly.Left, y * SubStep + poly.Bottom].Data;

                    var neighbors = new HashSet<PathPoly>();
                    tilePoly.Add(new PathPoly(grid, chunk.Origin, GetIndex(x, y), box, polyData, neighbors));
                }
            }
        }

        // Log.Debug($"Built breadcrumbs in {sw.Elapsed.TotalMilliseconds}ms");
        SendBreadcrumbs(chunk, grid);
    }

    /// <summary>
    /// Clears all of the polygons on a tile.
    /// </summary>
    private void ClearTilePolys(List<PathPoly> polys)
    {
        foreach (var poly in polys)
        {
            ClearPoly(poly);
        }

        polys.Clear();
    }

    /// <summary>
    /// Clears a polygon and invalidates its flags if anyone still has a reference to it.
    /// </summary>
    private void ClearPoly(PathPoly poly)
    {
        foreach (var neighbor in poly.Neighbors)
        {
            neighbor.Neighbors.Remove(poly);
        }

        // If any paths have a ref to it let them know that the class is no longer a valid node.
        poly.Data.Flags = PathfindingBreadcrumbFlag.Invalid;
        poly.Neighbors.Clear();
    }

    private void ClearOldPolys(GridPathfindingChunk chunk)
    {
        // Can't do this in BuildBreadcrumbs because it mutates neighbors
        // but also we need this entirely done before BuildNavmesh
        var chunkPolys = chunk.Polygons;
        var bufferPolygons = chunk.BufferPolygons;

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var index = x * ChunkSize + y;
                var polys = bufferPolygons[index];
                var existing = chunkPolys[index];

                var isEquivalent = true;

                if (polys.Count == existing.Count)
                {
                    // May want to update damage or the likes if it's different but not invalidate the ref.
                    for (var i = 0; i < existing.Count; i++)
                    {
                        var ePoly = existing[i];
                        var poly = polys[i];

                        if (!ePoly.IsEquivalent(poly))
                        {
                            isEquivalent = false;
                            break;
                        }

                        ePoly.Data.Damage = poly.Data.Damage;
                    }

                    if (isEquivalent)
                        continue;
                }

                ClearTilePolys(existing);
                existing.AddRange(polys);
            }
        }
    }

    private void BuildNavmesh(GridPathfindingChunk chunk, Entity<GridPathfindingComponent> pathfinding)
    {
        var sw = new Stopwatch();
        sw.Start();

        var chunkPolys = chunk.Polygons;
        var component = pathfinding.Comp;
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(-1, 0), out var leftChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(0, -1), out var bottomChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(1, 0), out var rightChunk);
        component.Chunks.TryGetValue(chunk.Origin + new Vector2i(0, 1), out var topChunk);

        // Now we can get the neighbors for our tile polys
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var index = GetIndex(x, y);
                var tile = chunkPolys[index];

                for (byte i = 0; i < tile.Count; i++)
                {
                    var poly = tile[i];
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

                        AddNeighbors(poly, neighbor);
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
                            var neighborIndex = GetIndex(neighborX, neighborY);
                            List<PathPoly> neighborTile;

                            if (neighborX < 0)
                            {
                                if (leftChunk == null)
                                    continue;

                                neighborX = ChunkSize - 1;
                                neighborIndex = GetIndex(neighborX, neighborY);
                                neighborTile = leftChunk.Polygons[neighborIndex];
                            }
                            else if (neighborY < 0)
                            {
                                if (bottomChunk == null)
                                    continue;

                                neighborY = ChunkSize - 1;
                                neighborIndex = GetIndex(neighborX, neighborY);
                                neighborTile = bottomChunk.Polygons[neighborIndex];
                            }
                            else if (neighborX >= ChunkSize)
                            {
                                if (rightChunk == null)
                                    continue;

                                neighborX = 0;
                                neighborIndex = GetIndex(neighborX, neighborY);
                                neighborTile = rightChunk.Polygons[neighborIndex];
                            }
                            else if (neighborY >= ChunkSize)
                            {
                                if (topChunk == null)
                                    continue;

                                neighborY = 0;
                                neighborIndex = GetIndex(neighborX, neighborY);
                                neighborTile = topChunk.Polygons[neighborIndex];
                            }
                            else
                            {
                                neighborTile = chunkPolys[neighborIndex];
                            }

                            for (byte j = 0; j < neighborTile.Count; j++)
                            {
                                var neighbor = neighborTile[j];
                                var enlargedNeighbor = neighbor.Box.Enlarged(StepOffset);
                                var overlap = Box2.Area(enlarged.Intersect(enlargedNeighbor));

                                // Need to ensure they intersect by at least 2 tiles.
                                if (overlap <= 0.5f / SubStep)
                                    continue;

                                AddNeighbors(poly, neighbor);
                            }
                        }
                    }
                }
            }
        }

        // Log.Debug($"Built navmesh in {sw.Elapsed.TotalMilliseconds}ms");
        SendPolys(chunk, pathfinding, chunkPolys);
    }

    private void AddNeighbors(PathPoly polyA, PathPoly polyB)
    {
        DebugTools.Assert((polyA.Data.Flags & PathfindingBreadcrumbFlag.Invalid) == 0x0);
        DebugTools.Assert((polyB.Data.Flags & PathfindingBreadcrumbFlag.Invalid) == 0x0);
        polyA.Neighbors.Add(polyB);
        polyB.Neighbors.Add(polyA);
    }
}
