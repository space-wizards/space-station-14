using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.NPC;
using Robust.Server.Player;
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

        private readonly Dictionary<ICommonSession, PathfindingDebugMode> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeGrid();
            SubscribeNetworkEvent<RequestPathfindingBreadcrumbsMessage>(OnBreadcrumbs);
        }

        private void OnBreadcrumbs(RequestPathfindingBreadcrumbsMessage msg, EntitySessionEventArgs args)
        {
            var pSession = (IPlayerSession) args.SenderSession;

            if (!_adminManager.HasAdminFlag(pSession, AdminFlags.Debug))
            {
                return;
            }

            var sessions = _subscribedSessions.GetOrNew(args.SenderSession);

            if ((sessions & PathfindingDebugMode.Breadcrumbs) != 0x0)
            {
                return;
            }

            sessions |= PathfindingDebugMode.Breadcrumbs;
            _subscribedSessions[args.SenderSession] = sessions;
            SendBreadcrumbs(pSession);
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
                    var crumbs = new List<PathfindingBreadcrumb>();
                    crumbs.AddRange(chunk.Value.Points);

                    msg.Breadcrumbs[comp.Owner].Add(chunk.Key, crumbs);
                }
            }

            RaiseNetworkEvent(msg, pSession.ConnectedClient);
        }
    }
}
