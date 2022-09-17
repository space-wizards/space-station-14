using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.NPC;
using Robust.Server.Player;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
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
        [Dependency] private readonly FixtureSystem _fixtures = default!;

        private readonly Dictionary<ICommonSession, PathfindingDebugMode> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeGrid();
            SubscribeNetworkEvent<RequestPathfindingDebugMessage>(OnBreadcrumbs);
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

            if ((sessions & PathfindingDebugMode.Breadcrumbs) != 0x0)
            {
                SendBreadcrumbs(pSession);
            }
        }

        private void SendBreadcrumbs()
        {
            foreach (var session in _subscribedSessions)
            {
                if ((session.Value & PathfindingDebugMode.Breadcrumbs) == 0)
                    continue;

                SendBreadcrumbs(session.Key);
            }
        }

        private void SendBreadcrumbs(ICommonSession pSession)
        {
            var msg = new PathfindingBreadcrumbsMessage();

            foreach (var comp in EntityQuery<GridPathfindingComponent>(true))
            {
                msg.Breadcrumbs.Add(comp.Owner, new Dictionary<Vector2i, List<PathfindingBreadcrumb>>(comp.Chunks.Count));

                foreach (var chunk in comp.Chunks)
                {
                    var data = GetData(chunk.Value);
                    msg.Breadcrumbs[comp.Owner].Add(chunk.Key, data);
                }
            }

            RaiseNetworkEvent(msg, pSession.ConnectedClient);
        }

        private void SendBreadcrumbs(GridPathfindingChunk chunk, EntityUid gridUid)
        {
            var msg = new PathfindingBreadcrumbsRefreshMessage()
            {
                Origin = chunk.Origin,
                GridUid = gridUid,
                Data = GetData(chunk),
            };

            foreach (var session in _subscribedSessions)
            {
                if ((session.Value & PathfindingDebugMode.Breadcrumbs) == 0x0)
                    continue;

                RaiseNetworkEvent(msg, session.Key.ConnectedClient);
            }
        }

        private List<PathfindingBreadcrumb> GetData(GridPathfindingChunk chunk)
        {
            var crumbs = new List<PathfindingBreadcrumb>(chunk.Points.Length * chunk.Points.Length);

            for (var x = 0; x < ChunkSize * SubStep; x++)
            {
                for (var y = 0; y < ChunkSize * SubStep; y++)
                {
                    crumbs.Add(chunk.Points[x, y]);
                }
            }

            return crumbs;
        }
    }
}
