using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Generic;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.ActionBlocker;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.AI.Utility.Actions.Idle
{
    /// <summary>
    /// Will move to a random spot close by
    /// </summary>
    public sealed class WanderAndWait : UtilityAction
    {
        public override bool CanOverride => false;
        public override float Bonus => IdleBonus;

        public WanderAndWait(IEntity owner) : base(owner)
        {
            // TODO: Need a Success method that gets called to update context (e.g. when we last did X)
        }

        public override void SetupOperators(Blackboard context)
        {
            var randomGrid = FindRandomGrid();
            float waitTime;
            if (randomGrid != GridCoordinates.InvalidGrid)
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                waitTime = random.NextFloat() * 10;
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

        protected override Consideration[] Considerations { get; } = {
            new CanMoveCon(
                new BoolCurve())
            // Last wander? If we also want to sit still
        };

        private GridCoordinates FindRandomGrid()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(Owner.Transform.GridID);

            // Just find a random spot in bounds
            // If the grid's a single-tile wide but really tall this won't really work but eh future problem
            var gridBounds = grid.WorldBounds;
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var newPosition = gridBounds.BottomLeft + new Vector2(
                                  robustRandom.Next((int) gridBounds.Width),
                                  robustRandom.Next((int) gridBounds.Height));
            // Conversions blah blah
            var mapIndex = grid.WorldToTile(grid.LocalToWorld(newPosition));
            // Didn't find one? Fuck it we're not walkin' into space
            if (grid.GetTileRef(mapIndex).Tile.IsEmpty)
            {
                return GridCoordinates.InvalidGrid;
            }
            var target = grid.GridTileToLocal(mapIndex);

            return target;
        }
    }
}
