using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.NPC;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
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
        [Dependency] private readonly FixtureSystem _fixtures = default!;

        private ISawmill _sawmill = default!;

        private readonly Dictionary<ICommonSession, PathfindingDebugMode> _subscribedSessions = new();

        private readonly List<PathRequest> _pathRequests = new(PathTickLimit);

        private static readonly TimeSpan PathTime = TimeSpan.FromMilliseconds(3);

        /// <summary>
        /// How many paths we can process in a single tick.
        /// </summary>
        private const int PathTickLimit = 256;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("nav");
            InitializeGrid();
            SubscribeNetworkEvent<RequestPathfindingDebugMessage>(OnBreadcrumbs);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _subscribedSessions.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateGrid();
            var graphs = EntityQuery<GridPathfindingComponent>(true).ToDictionary(o => o.Owner);
            _stopwatch.Restart();
            var results = new PathResult[_pathRequests.Count];

            Parallel.For(0, _pathRequests.Count, i =>
            {
                // If we're over the limit (either time-sliced or hard cap).
                if (i >= PathTickLimit || _stopwatch.Elapsed >= PathTime)
                    return;

                results[i] = UpdatePath(graphs, _pathRequests[i]);
            });

            // then, single-threaded cleanup.
            for (var i = 0; i < Math.Min(_pathRequests.Count, PathTickLimit); i++)
            {
                var path = _pathRequests[i];
                var result = results[i];

                if (path.Task.Exception != null)
                {
                    _pathRequests.RemoveAt(i);
                    throw path.Task.Exception;
                }

                switch (result)
                {
                    case PathResult.PartialPath:
                    case PathResult.Path:
                    case PathResult.NoPath:
                        SendDebug(path);
                        // Don't use RemoveSwap because we still want to try and process them in order.
                        _pathRequests.RemoveAt(i);
                        path.Tcs.SetResult(result);
                        break;
                }
            }
        }

        public async Task<PathResultEvent> GetPath(
            EntityUid entity,
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken)
        {
            return await GetPath(start, end, range, cancelToken);
        }

        /// <summary>
        /// Asynchronously gets a path.
        /// </summary>
        public async Task<PathResultEvent> GetPath(
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken)
        {
            // Don't allow the caller to pass in the request in case they try to do something with its data.
            var request = new PathRequest(start, end, PathFlags.None, cancelToken);
            _pathRequests.Add(request);

            await request.Task;

            if (request.Task.Exception != null)
            {
                throw request.Task.Exception;
            }

            // Same context as do_after and not synchronously blocking soooo
#pragma warning disable RA0004
            var ev = new PathResultEvent(request.Task.Result, request.Path);
#pragma warning restore RA0004

            return ev;
        }

        /// <summary>
        /// Raises the pathfinding result event on the entity when finished.
        /// </summary>
        public async void GetPathEvent(
            EntityUid uid,
            EntityCoordinates start,
            EntityCoordinates end,
            float range,
            CancellationToken cancelToken)
        {
            var path = await GetPath(start, end, range, cancelToken);
            RaiseLocalEvent(uid, path);
        }

        public PathPoly? GetPoly(PathPolyRef polyRef, GridPathfindingComponent? component = null)
        {
            if (!Resolve(polyRef.GraphUid, ref component, false))
                return null;

            // TODO Validate
            return component.GetNeighbor(polyRef);
        }

        /// <summary>
        /// Gets the relevant poly for the specified coordinates if it exists.
        /// </summary>
        public PathPoly? GetPoly(EntityCoordinates coordinates)
        {
            var gridUid = coordinates.GetGridUid(EntityManager);

            if (!TryComp<GridPathfindingComponent>(gridUid, out var comp) ||
                !TryComp<TransformComponent>(gridUid, out var xform))
            {
                return null;
            }

            var localPos = xform.InvWorldMatrix.Transform(coordinates.ToMapPos(EntityManager));
            var origin = GetOrigin(localPos);

            if (!TryGetChunk(origin, comp, out var chunk))
                return null;

            var chunkPos = new Vector2(MathHelper.Mod(localPos.X, ChunkSize), MathHelper.Mod(localPos.Y, ChunkSize));
            var polys = chunk.Polygons[(int) chunkPos.X, (int) chunkPos.Y];

            foreach (var poly in polys)
            {
                if (!poly.Box.Contains(localPos))
                    continue;

                return poly;
            }

            return null;
        }

        public PathPolyRef? GetPolyRef(EntityCoordinates coordinates)
        {
            var gridUid = coordinates.GetGridUid(EntityManager);

            if (!TryComp<GridPathfindingComponent>(gridUid, out var comp) ||
                !TryComp<TransformComponent>(gridUid, out var xform))
            {
                return null;
            }

            var localPos = xform.InvWorldMatrix.Transform(coordinates.ToMapPos(EntityManager));
            var origin = GetOrigin(localPos);

            if (!TryGetChunk(origin, comp, out var chunk))
                return null;

            var chunkPos = new Vector2i((int) MathHelper.Mod(localPos.X, ChunkSize), (int) MathHelper.Mod(localPos.Y, ChunkSize));
            var polys = chunk.Polygons[chunkPos.X, chunkPos.Y];

            for (var i = 0; i < polys.Count; i++)
            {
                var poly = polys[i];

                if (!poly.Box.Contains(localPos))
                    continue;

                return new PathPolyRef(gridUid.Value, origin, GetIndex(chunkPos.X, chunkPos.Y), (byte) i);
            }

            return null;
        }

        #region Debug handlers

        private void SendDebug(PathRequest request)
        {
            if (_subscribedSessions.Count == 0)
                return;

            foreach (var session in _subscribedSessions)
            {
                if ((session.Value & PathfindingDebugMode.Routes) == 0x0)
                    continue;

                var polys = new List<PathPoly>(request.Polys.Count);

                foreach (var polyRef in polys)
                {

                }

                RaiseNetworkEvent(new PathRouteMessage(request.Polys.Select(o => GetPoly(o)).ToList(), new Dictionary<PathPolyRef, float>()), session.Key.ConnectedClient);
            }
        }

        private void OnBreadcrumbs(RequestPathfindingDebugMessage msg, EntitySessionEventArgs args)
        {
            var pSession = (IPlayerSession) args.SenderSession;

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

        private void SendBreadcrumbs(ICommonSession pSession)
        {
            var msg = new PathBreadcrumbsMessage();

            foreach (var comp in EntityQuery<GridPathfindingComponent>(true))
            {
                msg.Breadcrumbs.Add(comp.Owner, new Dictionary<Vector2i, List<PathfindingBreadcrumb>>(comp.Chunks.Count));

                foreach (var chunk in comp.Chunks)
                {
                    var data = GetCrumbs(chunk.Value);
                    msg.Breadcrumbs[comp.Owner].Add(chunk.Key, data);
                }
            }

            RaiseNetworkEvent(msg, pSession.ConnectedClient);
        }

        private void SendPolys(ICommonSession pSession)
        {
            var msg = new PathPolysMessage();

            foreach (var comp in EntityQuery<GridPathfindingComponent>(true))
            {
                msg.Polys.Add(comp.Owner, new Dictionary<Vector2i, Dictionary<Vector2i, List<PathPoly>>>(comp.Chunks.Count));

                foreach (var chunk in comp.Chunks)
                {
                    var data = GetPolys(chunk.Value);
                    msg.Polys[comp.Owner].Add(chunk.Key, data);
                }
            }

            RaiseNetworkEvent(msg, pSession.ConnectedClient);
        }

        private void SendBreadcrumbs(GridPathfindingChunk chunk, EntityUid gridUid)
        {
            if (_subscribedSessions.Count == 0)
                return;

            var msg = new PathBreadcrumbsRefreshMessage()
            {
                Origin = chunk.Origin,
                GridUid = gridUid,
                Data = GetCrumbs(chunk),
            };

            foreach (var session in _subscribedSessions)
            {
                if (!IsCrumb(session.Value))
                    continue;

                RaiseNetworkEvent(msg, session.Key.ConnectedClient);
            }
        }

        private void SendPolys(GridPathfindingChunk chunk, EntityUid gridUid,
            List<PathPoly>[,] tilePolys)
        {
            if (_subscribedSessions.Count == 0)
                return;

            var data = new Dictionary<Vector2i, List<PathPoly>>(tilePolys.Length);
            var extent = Math.Sqrt(tilePolys.Length);

            for (var x = 0; x < extent; x++)
            {
                for (var y = 0; y < extent; y++)
                {
                    data[new Vector2i(x, y)] = tilePolys[x, y];
                }
            }

            var msg = new PathPolysRefreshMessage()
            {
                Origin = chunk.Origin,
                GridUid = gridUid,
                Polys = data,
            };

            foreach (var session in _subscribedSessions)
            {
                if (!IsPoly(session.Value))
                    continue;

                RaiseNetworkEvent(msg, session.Key.ConnectedClient);
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

        private Dictionary<Vector2i, List<PathPoly>> GetPolys(GridPathfindingChunk chunk)
        {
            var polys = new Dictionary<Vector2i, List<PathPoly>>(chunk.Polygons.Length);

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    polys[new Vector2i(x, y)] = chunk.Polygons[x, y];
                }
            }

            return polys;
        }

        #endregion
    }
}
