using System.Buffers;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Destructible;
using Content.Server.NPC.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Climbing.Components;
using Content.Shared.Doors.Components;
using Content.Shared.NPC;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding
{
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public sealed partial class PathfindingSystem : SharedPathfindingSystem
    {
        /*
         * I have spent many hours looking at what pathfinding to use
         * Ideally we would be able to use something grid based with hierarchy, but the problem is
         * we also have triangular / diagonal walls and thindows which makes that not exactly feasible
         * Recast is also overkill for our usecase, plus another lib, hence you get this.
         *
         * See PathfindingSystem.Grid for a description of the grid implementation.
         */

        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IParallelManager _parallel = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DestructibleSystem _destructible = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly SharedMapSystem _maps = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private readonly Dictionary<ICommonSession, PathfindingDebugMode> _subscribedSessions = new();

        [ViewVariables]
        private readonly List<PathRequest> _pathRequests = new(PathTickLimit);

        private static readonly TimeSpan PathTime = TimeSpan.FromMilliseconds(3);

        /// <summary>
        /// How many paths we can process in a single tick.
        /// </summary>
        private const int PathTickLimit = 256;

        private int _portalIndex;
        private readonly Dictionary<int, PathPortal> _portals = new();

        private EntityQuery<AccessReaderComponent> _accessQuery;
        private EntityQuery<DestructibleComponent> _destructibleQuery;
        private EntityQuery<DoorComponent> _doorQuery;
        private EntityQuery<ClimbableComponent> _climbableQuery;
        private EntityQuery<FixturesComponent> _fixturesQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        public override void Initialize()
        {
            base.Initialize();

            _accessQuery = GetEntityQuery<AccessReaderComponent>();
            _destructibleQuery = GetEntityQuery<DestructibleComponent>();
            _doorQuery = GetEntityQuery<DoorComponent>();
            _climbableQuery = GetEntityQuery<ClimbableComponent>();
            _fixturesQuery = GetEntityQuery<FixturesComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

            _playerManager.PlayerStatusChanged += OnPlayerChange;
            InitializeGrid();
            SubscribeNetworkEvent<RequestPathfindingDebugMessage>(OnBreadcrumbs);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _subscribedSessions.Clear();
            _playerManager.PlayerStatusChanged -= OnPlayerChange;
            _transform.OnGlobalMoveEvent -= OnMoveEvent;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _parallel.ParallelProcessCount,
            };

            UpdateGrid(options);
            _stopwatch.Restart();
            var amount = Math.Min(PathTickLimit, _pathRequests.Count);
            var results = ArrayPool<PathResult>.Shared.Rent(amount);


            Parallel.For(0, amount, options, i =>
            {
                // If we're over the limit (either time-sliced or hard cap).
                if (_stopwatch.Elapsed >= PathTime)
                {
                    results[i] = PathResult.Continuing;
                    return;
                }

                var request = _pathRequests[i];

                try
                {
                    switch (request)
                    {
                        case AStarPathRequest astar:
                            results[i] = UpdateAStarPath(astar);
                            break;
                        case BFSPathRequest bfs:
                            results[i] = UpdateBFSPath(_random, bfs);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception)
                {
                    results[i] = PathResult.NoPath;
                    throw;
                }
            });

            var offset = 0;

            // then, single-threaded cleanup.
            for (var i = 0; i < amount; i++)
            {
                var resultIndex = i + offset;
                var path = _pathRequests[resultIndex];
                var result = results[i];

                if (path.Task.Exception != null)
                {
                    throw path.Task.Exception;
                }

                switch (result)
                {
                    case PathResult.Continuing:
                        break;
                    case PathResult.PartialPath:
                    case PathResult.Path:
                    case PathResult.NoPath:
                        SendDebug(path);
                        // Don't use RemoveSwap because we still want to try and process them in order.
                        _pathRequests.RemoveAt(resultIndex);
                        offset--;
                        path.Tcs.SetResult(result);
                        SendRoute(path);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            ArrayPool<PathResult>.Shared.Return(results);
        }

        /// <summary>
        /// Creates neighbouring edges at both locations, each leading to the other.
        /// </summary>
        public bool TryCreatePortal(EntityCoordinates coordsA, EntityCoordinates coordsB, out int handle)
        {
            var mapUidA = _transform.GetMap(coordsA);
            var mapUidB = _transform.GetMap(coordsB);
            handle = -1;

            if (mapUidA != mapUidB || mapUidA == null)
            {
                return false;
            }

            var gridUidA = _transform.GetGrid(coordsA);
            var gridUidB = _transform.GetGrid(coordsB);

            if (!TryComp<GridPathfindingComponent>(gridUidA, out var gridA) ||
                !TryComp<GridPathfindingComponent>(gridUidB, out var gridB))
            {
                return false;
            }

            handle = _portalIndex++;
            var portal = new PathPortal(handle, coordsA, coordsB);
            _portals[handle] = portal;
            var originA = GetOrigin(coordsA, gridUidA.Value);
            var originB = GetOrigin(coordsB, gridUidB.Value);

            gridA.PortalLookup.Add(portal, originA);
            gridB.PortalLookup.Add(portal, originB);

            var chunkA = GetChunk(originA, gridUidA.Value);
            var chunkB = GetChunk(originB, gridUidB.Value);
            chunkA.Portals.Add(portal);
            chunkB.Portals.Add(portal);

            // TODO: You already have the chunks
            DirtyChunk(gridUidA.Value, coordsA);
            DirtyChunk(gridUidB.Value, coordsB);

            return true;
        }

        public bool RemovePortal(int handle)
        {
            if (!_portals.TryGetValue(handle, out var portal))
            {
                return false;
            }

            _portals.Remove(handle);

            var gridUidA = _transform.GetGrid(portal.CoordinatesA);
            var gridUidB = _transform.GetGrid(portal.CoordinatesB);

            if (!TryComp<GridPathfindingComponent>(gridUidA, out var gridA) ||
                !TryComp<GridPathfindingComponent>(gridUidB, out var gridB))
            {
                return false;
            }

            gridA.PortalLookup.Remove(portal);
            gridB.PortalLookup.Remove(portal);
            var chunkA = GetChunk(GetOrigin(portal.CoordinatesA, gridUidA.Value), gridUidA.Value, gridA);
            var chunkB = GetChunk(GetOrigin(portal.CoordinatesB, gridUidB.Value), gridUidB.Value, gridB);
            chunkA.Portals.Remove(portal);
            chunkB.Portals.Remove(portal);
            DirtyChunk(gridUidA.Value, portal.CoordinatesA);
            DirtyChunk(gridUidB.Value, portal.CoordinatesB);

            return true;
        }

        public async Task<PathResultEvent> GetRandomPath(
            EntityUid entity,
            float maxRange,
            CancellationToken cancelToken,
            int limit = 40,
            PathFlags flags = PathFlags.None)
        {
            if (!TryComp(entity, out TransformComponent? start))
                return new PathResultEvent(PathResult.NoPath, new List<PathPoly>());

            var layer = 0;
            var mask = 0;

            if (TryComp<FixturesComponent>(entity, out var fixtures))
            {
                (layer, mask) = _physics.GetHardCollision(entity, fixtures);
            }

            var request = new BFSPathRequest(maxRange, limit, start.Coordinates, flags, layer, mask, cancelToken);
            var path = await GetPath(request);

            if (path.Result != PathResult.Path)
                return new PathResultEvent(PathResult.NoPath, new List<PathPoly>());

            return new PathResultEvent(PathResult.Path, path.Path);
        }

        /// <summary>
        /// Gets the estimated distance from the entity to the target node.
        /// </summary>
        public async Task<float?> GetPathDistance(
            EntityUid entity,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            if (!TryComp(entity, out TransformComponent? start))
                return null;

            var request = GetRequest(entity, start.Coordinates, end, range, cancelToken, flags);
            var path = await GetPath(request);

            if (path.Result != PathResult.Path)
                return null;

            if (path.Path.Count == 0)
                return 0f;

            var distance = 0f;
            var lastNode = path.Path[0];

            for (var i = 1; i < path.Path.Count; i++)
            {
                var node = path.Path[i];
                distance += GetTileCost(request, lastNode, node);
            }

            return distance;
        }

        public async Task<PathResultEvent> GetPath(
            EntityUid entity,
            EntityUid target,
            float range,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            if (!TryComp(entity, out TransformComponent? xform) ||
                !TryComp(target, out TransformComponent? targetXform))
                return new PathResultEvent(PathResult.NoPath, new List<PathPoly>());

            var request = GetRequest(entity, xform.Coordinates, targetXform.Coordinates, range, cancelToken, flags);
            return await GetPath(request);
        }

        public async Task<PathResultEvent> GetPath(
            EntityUid entity,
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            var request = GetRequest(entity, start, end, range, cancelToken, flags);
            return await GetPath(request);
        }

        /// <summary>
        /// Gets a path in a thread-safe way.
        /// </summary>
        public async Task<PathResultEvent> GetPathSafe(
            EntityUid entity,
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            var request = GetRequest(entity, start, end, range, cancelToken, flags);
            return await GetPath(request, true);
        }

        /// <summary>
        /// Asynchronously gets a path.
        /// </summary>
        public async Task<PathResultEvent> GetPath(
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            int layer,
            int mask,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            // Don't allow the caller to pass in the request in case they try to do something with its data.
            var request = new AStarPathRequest(start, end, flags, range, layer, mask, cancelToken);
            return await GetPath(request);
        }

        /// <summary>
        /// Raises the pathfinding result event on the entity when finished.
        /// </summary>
        public async void GetPathEvent(
            EntityUid uid,
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken,
            PathFlags flags = PathFlags.None)
        {
            var path = await GetPath(uid, start, end, range, cancelToken);
            RaiseLocalEvent(uid, path);
        }

        /// <summary>
        /// Gets the relevant poly for the specified coordinates if it exists.
        /// </summary>
        public PathPoly? GetPoly(EntityCoordinates coordinates)
        {
            var gridUid = _transform.GetGrid(coordinates);

            if (!TryComp<GridPathfindingComponent>(gridUid, out var comp) ||
                !TryComp(gridUid, out TransformComponent? xform))
            {
                return null;
            }

            var localPos = Vector2.Transform(_transform.ToMapCoordinates(coordinates).Position, _transform.GetInvWorldMatrix(xform));
            var origin = GetOrigin(localPos);

            if (!TryGetChunk(origin, comp, out var chunk))
                return null;

            var chunkPos = new Vector2(MathHelper.Mod(localPos.X, ChunkSize), MathHelper.Mod(localPos.Y, ChunkSize));
            var polys = chunk.Polygons[(int)chunkPos.X * ChunkSize + (int)chunkPos.Y];

            foreach (var poly in polys)
            {
                if (!poly.Box.Contains(localPos))
                    continue;

                return poly;
            }

            return null;
        }

        private PathRequest GetRequest(EntityUid entity, EntityCoordinates start, EntityCoordinates end, float range, CancellationToken cancelToken, PathFlags flags)
        {
            var layer = 0;
            var mask = 0;

            if (TryComp<FixturesComponent>(entity, out var fixtures))
            {
                (layer, mask) = _physics.GetHardCollision(entity, fixtures);
            }

            return new AStarPathRequest(start, end, flags, range, layer, mask, cancelToken);
        }

        public PathFlags GetFlags(EntityUid uid)
        {
            if (!_npc.TryGetNpc(uid, out var npc))
            {
                return PathFlags.None;
            }

            return GetFlags(npc.Blackboard);
        }

        public PathFlags GetFlags(NPCBlackboard blackboard)
        {
            var flags = PathFlags.None;

            if (blackboard.TryGetValue<bool>(NPCBlackboard.NavPry, out var pry, EntityManager) && pry)
            {
                flags |= PathFlags.Prying;
            }

            if (blackboard.TryGetValue<bool>(NPCBlackboard.NavSmash, out var smash, EntityManager) && smash)
            {
                flags |= PathFlags.Smashing;
            }

            if (blackboard.TryGetValue<bool>(NPCBlackboard.NavClimb, out var climb, EntityManager) && climb)
            {
                flags |= PathFlags.Climbing;
            }

            if (blackboard.TryGetValue<bool>(NPCBlackboard.NavInteract, out var interact, EntityManager) && interact)
            {
                flags |= PathFlags.Interact;
            }

            return flags;
        }

        private async Task<PathResultEvent> GetPath(
            PathRequest request, bool safe = false)
        {
            // We could maybe try an initial quick run to avoid forcing time-slicing over ticks.
            // For now it seems okay and it shouldn't block on 1 NPC anyway.

            if (safe)
            {
                lock (_pathRequests)
                {
                    _pathRequests.Add(request);
                }
            }
            else
            {
                _pathRequests.Add(request);
            }

            await request.Task;

            if (request.Task.Exception != null)
            {
                throw request.Task.Exception;
            }

            if (!request.Task.IsCompletedSuccessfully)
            {
                return new PathResultEvent(PathResult.NoPath, new List<PathPoly>());
            }

            // Same context as do_after and not synchronously blocking soooo
#pragma warning disable RA0004
            var ev = new PathResultEvent(request.Task.Result, request.Polys);
#pragma warning restore RA0004

            return ev;
        }

        #region Debug handlers

        private DebugPathPoly GetDebugPoly(PathPoly poly)
        {
            // Create fake neighbors for it
            var neighbors = new List<NetCoordinates>(poly.Neighbors.Count);

            foreach (var neighbor in poly.Neighbors)
            {
                neighbors.Add(GetNetCoordinates(neighbor.Coordinates));
            }

            return new DebugPathPoly()
            {
                GraphUid = GetNetEntity(poly.GraphUid),
                ChunkOrigin = poly.ChunkOrigin,
                TileIndex = poly.TileIndex,
                Box = poly.Box,
                Data = poly.Data,
                Neighbors = neighbors,
            };
        }

        private void SendDebug(PathRequest request)
        {
            if (_subscribedSessions.Count == 0)
                return;

            foreach (var session in _subscribedSessions)
            {
                if ((session.Value & PathfindingDebugMode.Routes) == 0x0)
                    continue;

                RaiseNetworkEvent(new PathRouteMessage(request.Polys.Select(GetDebugPoly).ToList(), new Dictionary<DebugPathPoly, float>()), session.Key.Channel);
            }
        }

        private void OnBreadcrumbs(RequestPathfindingDebugMessage msg, EntitySessionEventArgs args)
        {
            var pSession = args.SenderSession;

            if (!_adminManager.HasAdminFlag(pSession, AdminFlags.Debug))
            {
                return;
            }

            var sessions = _subscribedSessions.GetOrNew(args.SenderSession);

            if (msg.Mode == PathfindingDebugMode.None)
            {
                _subscribedSessions.Remove(args.SenderSession);
                return;
            }

            sessions = msg.Mode;
            _subscribedSessions[args.SenderSession] = sessions;

            if (IsCrumb(sessions))
            {
                SendBreadcrumbs(pSession);
            }

            if (IsPoly(sessions))
            {
                SendPolys(pSession);
            }
        }

        private bool IsCrumb(PathfindingDebugMode mode)
        {
            return (mode & (PathfindingDebugMode.Breadcrumbs | PathfindingDebugMode.Crumb)) != 0x0;
        }

        private bool IsPoly(PathfindingDebugMode mode)
        {
            return (mode & (PathfindingDebugMode.Chunks | PathfindingDebugMode.Polys | PathfindingDebugMode.Poly | PathfindingDebugMode.PolyNeighbors)) != 0x0;
        }

        private bool IsRoute(PathfindingDebugMode mode)
        {
            return (mode & (PathfindingDebugMode.Routes | PathfindingDebugMode.RouteCosts)) != 0x0;
        }

        private void SendBreadcrumbs(ICommonSession pSession)
        {
            var msg = new PathBreadcrumbsMessage();

            var query = AllEntityQuery<GridPathfindingComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var netGrid = GetNetEntity(uid);

                msg.Breadcrumbs.Add(netGrid, new Dictionary<Vector2i, List<PathfindingBreadcrumb>>(comp.Chunks.Count));

                foreach (var chunk in comp.Chunks)
                {
                    var data = GetCrumbs(chunk.Value);
                    msg.Breadcrumbs[netGrid].Add(chunk.Key, data);
                }
            }

            RaiseNetworkEvent(msg, pSession.Channel);
        }

        private void SendRoute(PathRequest request)
        {
            if (_subscribedSessions.Count == 0)
                return;

            var polys = new List<DebugPathPoly>();
            var costs = new Dictionary<DebugPathPoly, float>();

            foreach (var poly in request.Polys)
            {
                polys.Add(GetDebugPoly(poly));
            }

            foreach (var (poly, value) in request.CostSoFar)
            {
                costs.Add(GetDebugPoly(poly), value);
            }

            var msg = new PathRouteMessage(polys, costs);

            foreach (var session in _subscribedSessions)
            {
                if (!IsRoute(session.Value))
                    continue;

                RaiseNetworkEvent(msg, session.Key.Channel);
            }
        }

        private void SendPolys(ICommonSession pSession)
        {
            var msg = new PathPolysMessage();

            var query = AllEntityQuery<GridPathfindingComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var netGrid = GetNetEntity(uid);

                msg.Polys.Add(netGrid, new Dictionary<Vector2i, Dictionary<Vector2i, List<DebugPathPoly>>>(comp.Chunks.Count));

                foreach (var chunk in comp.Chunks)
                {
                    var data = GetPolys(chunk.Value);
                    msg.Polys[netGrid].Add(chunk.Key, data);
                }
            }

            RaiseNetworkEvent(msg, pSession.Channel);
        }

        private void SendBreadcrumbs(GridPathfindingChunk chunk, EntityUid gridUid)
        {
            if (_subscribedSessions.Count == 0)
                return;

            var msg = new PathBreadcrumbsRefreshMessage()
            {
                Origin = chunk.Origin,
                GridUid = GetNetEntity(gridUid),
                Data = GetCrumbs(chunk),
            };

            foreach (var session in _subscribedSessions)
            {
                if (!IsCrumb(session.Value))
                    continue;

                RaiseNetworkEvent(msg, session.Key.Channel);
            }
        }

        private void SendPolys(GridPathfindingChunk chunk, EntityUid gridUid,
            List<PathPoly>[] tilePolys)
        {
            if (_subscribedSessions.Count == 0)
                return;

            var data = new Dictionary<Vector2i, List<DebugPathPoly>>(tilePolys.Length);
            var extent = Math.Sqrt(tilePolys.Length);

            for (var x = 0; x < extent; x++)
            {
                for (var y = 0; y < extent; y++)
                {
                    var index = GetIndex(x, y);
                    data[new Vector2i(x, y)] = tilePolys[index].Select(GetDebugPoly).ToList();
                }
            }

            var msg = new PathPolysRefreshMessage()
            {
                Origin = chunk.Origin,
                GridUid = GetNetEntity(gridUid),
                Polys = data,
            };

            foreach (var session in _subscribedSessions)
            {
                if (!IsPoly(session.Value))
                    continue;

                RaiseNetworkEvent(msg, session.Key.Channel);
            }
        }

        private List<PathfindingBreadcrumb> GetCrumbs(GridPathfindingChunk chunk)
        {
            var crumbs = new List<PathfindingBreadcrumb>(chunk.Points.Length);
            const int extent = ChunkSize * SubStep;

            for (var x = 0; x < extent; x++)
            {
                for (var y = 0; y < extent; y++)
                {
                    crumbs.Add(chunk.Points[x, y]);
                }
            }

            return crumbs;
        }

        private Dictionary<Vector2i, List<DebugPathPoly>> GetPolys(GridPathfindingChunk chunk)
        {
            var polys = new Dictionary<Vector2i, List<DebugPathPoly>>(chunk.Polygons.Length);

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var index = GetIndex(x, y);
                    polys[new Vector2i(x, y)] = chunk.Polygons[index].Select(GetDebugPoly).ToList();
                }
            }

            return polys;
        }

        private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Connected || !_subscribedSessions.ContainsKey(e.Session))
                return;

            _subscribedSessions.Remove(e.Session);
        }

        #endregion
    }
}
