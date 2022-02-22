using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Generic;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Accessible;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.ActionBlocker;
using Content.Server.AI.WorldState;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.AI.Utility.Actions.Idle
{
    /// <summary>
    /// Will move to a random spot close by
    /// </summary>
    public sealed class WanderAndWait : UtilityAction
    {
        public override bool CanOverride => false;
        public override float Bonus => 1.0f;

        public override void SetupOperators(Blackboard context)
        {
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var randomGrid = FindRandomGrid(robustRandom);
            float waitTime;
            if (randomGrid != EntityCoordinates.Invalid)
            {
                waitTime = robustRandom.Next(3, 8);
            }
            else
            {
                waitTime = 0.0f;
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToGridOperator(Owner, randomGrid),
                new WaitOperator(waitTime),
            });
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanMoveCon>()
                    .BoolCurve(context),
            };
        }

        private EntityCoordinates FindRandomGrid(IRobustRandom robustRandom)
        {
            // Very inefficient (should weight each region by its node count) but better than the old system
            var reachableSystem = EntitySystem.Get<AiReachableSystem>();
            var reachableArgs = ReachableArgs.GetArgs(Owner);
            var entityRegion = reachableSystem.GetRegion(Owner);
            var reachableRegions = reachableSystem.GetReachableRegions(reachableArgs, entityRegion);

            // TODO: When SetupOperators can fail this should be null and fail the setup.
            if (reachableRegions.Count == 0)
            {
                return default;
            }

            var reachableNodes = new List<PathfindingNode>();

            foreach (var region in reachableRegions)
            {
                foreach (var node in region.Nodes)
                {
                    reachableNodes.Add(node);
                }
            }

            var targetNode = robustRandom.Pick(reachableNodes);
            var mapManager = IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID);
            var targetGrid = grid.GridTileToLocal(targetNode.TileRef.GridIndices);

            return targetGrid;
        }
    }
}
