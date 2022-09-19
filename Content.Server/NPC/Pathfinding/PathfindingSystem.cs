using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.NPC;
using Robust.Server.Player;
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
    }
}
