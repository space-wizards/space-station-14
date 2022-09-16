using Robust.Shared.Players;

namespace Content.Server.NPC.Pathfinding
{
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public sealed partial class PathfindingSystem : EntitySystem
    {
        /*
         * I have spent many hours looking at what pathfinding to use
         * Ideally we would be able to use something grid based with hierarchy, but the problem is
         * we also have triangular / diagonal walls and thindows which makes that not exactly feasible
         * Recast is also overkill for our usecase, plus another lib, hence you get this.
         *
         * See PathfindingSystem.Grid for a description of the grid implementation.
         */

        private HashSet<ICommonSession> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeGrid();
        }
    }
}
