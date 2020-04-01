using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.AI.Operators.Movement
{
    public class MoveToGridOperator : BaseMover
    {
        // Instance variables
        private IMapManager _mapManager;

        public MoveToGridOperator(IEntity owner, GridCoordinates gridPosition)
        {
            Setup(owner);
            TargetGrid = gridPosition;
            _mapManager = IoCManager.Resolve<IMapManager>();
            PathfindingProximity = 0.2f; // Accept no substitutes
        }

        public void UpdateTarget(GridCoordinates newTarget)
        {
            TargetGrid = newTarget;
            HaveArrived();
            GetRoute();
        }

        public override Outcome Execute(float frameTime)
        {
            base.Execute(frameTime);
            // TODO: Fix double-pathing. Is the problem with MoveToGridOperator?
            if (TargetGrid.GridID != Owner.Transform.GridID)
            {
                HaveArrived();
                return Outcome.Failed;
            }

            if (RouteJob != null)
            {
                if (RouteJob.Status != Status.Finished)
                {
                    return Outcome.Continuing;
                }
                ReceivedRoute();
                return Route.Count == 0 ? Outcome.Failed : Outcome.Continuing;
            }

            var targetRange = (TargetGrid.Position - Owner.Transform.GridPosition.Position).Length;

            // We there
            if (targetRange <= 1.5f)
            {
                HaveArrived();
                return Outcome.Success;
            }

            // No route
            if (Route.Count == 0 && RouteJob == null)
            {
                GetRoute();
                return Outcome.Continuing;
            }

            AntiStuck(frameTime);

            if (IsStuck)
            {
                return Outcome.Continuing;
            }

            if (TryMove())
            {
                return Outcome.Continuing;
            }

            if (Route.Count == 0 && targetRange > 1.5f)
            {
                HaveArrived();
                return Outcome.Failed;
            }

            var nextTile = Route.Dequeue();
            NextTile();
            NextGrid = _mapManager.GetGrid(nextTile.GridIndex).GridTileToLocal(nextTile.GridIndices);
            return Outcome.Continuing;
        }
    }
}
