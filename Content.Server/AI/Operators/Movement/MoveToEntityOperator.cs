using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToEntityOperator : BaseMover
    {
        // Instance
        private GridCoordinates _lastTargetPosition;
        private IMapManager _mapManager;

        // Input
        public IEntity Target { get; }
        public float DesiredRange { get; set; }

        public MoveToEntityOperator(IEntity owner, IEntity target, float desiredRange = 1.5f)
        {
            Setup(owner);
            Target = target;
            _mapManager = IoCManager.Resolve<IMapManager>();
            DesiredRange = desiredRange;
        }

        public override Outcome Execute(float frameTime)
        {
            var baseOutcome = base.Execute(frameTime);
            // TODO: Given this is probably the most common operator whatever speed boosts you can do here will be gucci
            // Could also look at running it every other tick.

            if (baseOutcome == Outcome.Failed ||
                Target == null ||
                Target.Deleted ||
                Target.Transform.GridID != Owner.Transform.GridID)
            {
                HaveArrived();
                return Outcome.Failed;
            }

            if (RouteJob != null)
            {
                if (RouteJob.Status != JobStatus.Finished)
                {
                    return Outcome.Continuing;
                }
                ReceivedRoute();
                return Route.Count == 0 ? Outcome.Failed : Outcome.Continuing;
            }

            var targetRange = (Target.Transform.GridPosition.Position - Owner.Transform.GridPosition.Position).Length;

            // If they move near us
            if (targetRange <= DesiredRange)
            {
                HaveArrived();
                return Outcome.Success;
            }

            // If the target's moved we may need to re-route.
            // First we'll check if they're near another tile on the existing route and if so
            // we can trim up until that point.
            if (_lastTargetPosition != default &&
                (Target.Transform.GridPosition.Position - _lastTargetPosition.Position).Length > 1.5f)
            {
                var success = false;
                // Technically it should be Route.Count - 1 but if the route's empty it'll throw
                var newRoute = new Queue<TileRef>(Route.Count);

                for (var i = 0; i < Route.Count; i++)
                {
                    var tile = Route.Dequeue();
                    newRoute.Enqueue(tile);
                    var tileGrid =  _mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);

                    // Don't use DesiredRange here or above in case it's smaller than a tile;
                    // when we get close we run straight at them anyway so it shooouullddd be okay...
                    if ((Target.Transform.GridPosition.Position - tileGrid.Position).Length < 1.5f)
                    {
                        success = true;
                        break;
                    }
                }

                if (success)
                {
                    Route = newRoute;
                    _lastTargetPosition = Target.Transform.GridPosition;
                    TargetGrid = Target.Transform.GridPosition;
                    return Outcome.Continuing;
                }

                _lastTargetPosition = default;
            }

            // If they move too far or no route
            if (_lastTargetPosition == default)
            {
                // If they're further we could try pathfinding from the furthest tile potentially?
                _lastTargetPosition = Target.Transform.GridPosition;
                TargetGrid = Target.Transform.GridPosition;
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

            // If we're really close just try bee-lining it?
            if (Route.Count == 0)
            {
                if (targetRange < 1.9f)
                {
                    // TODO: If they have a phat hitbox they could block us
                    NextGrid = TargetGrid;
                    return Outcome.Continuing;
                }
                if (targetRange > DesiredRange)
                {
                    HaveArrived();
                    return Outcome.Failed;
                }
            }

            var nextTile = Route.Dequeue();
            NextTile();
            NextGrid = _mapManager.GetGrid(nextTile.GridIndex).GridTileToLocal(nextTile.GridIndices);
            return Outcome.Continuing;
        }
    }
}
