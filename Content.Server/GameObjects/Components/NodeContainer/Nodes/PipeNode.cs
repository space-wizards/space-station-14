using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    [Node("PipeNode")]
    public class PipeNode : Node
    {
        [ViewVariables]
        private PipeDirection _pipeDirection;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipeDirection, "pipeDirection", PipeDirection.None);
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            foreach (CardinalDirection direction in Enum.GetValues(typeof(CardinalDirection)))
            {
                PipeDirectionFromCardinal(direction, out var ownNeededConnection, out var theirNeededConnection);
                if ((ownNeededConnection & _pipeDirection) == PipeDirection.None)
                {
                    continue;
                }
                var directionalNodesInDirection = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir((Direction) direction)
                    .Select(entity => entity.TryGetComponent<NodeContainerComponent>(out var container) ? container : null)
                    .Where(container => container != null)
                    .SelectMany(container => container.Nodes)
                    .OfType<PipeNode>()
                    .Where(node => node != null && node != this);
                foreach (var directionalNode in directionalNodesInDirection)
                {
                    if ((directionalNode._pipeDirection & theirNeededConnection) != PipeDirection.None)
                    {
                        yield return directionalNode;
                    }
                }
            }
        }

        private void PipeDirectionFromCardinal(CardinalDirection direction, out PipeDirection sameDir, out PipeDirection oppDir)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    sameDir = PipeDirection.Up;
                    oppDir = PipeDirection.Down;
                    break;
                case CardinalDirection.South:
                    sameDir = PipeDirection.Down;
                    oppDir = PipeDirection.Up;
                    break;
                case CardinalDirection.East:
                    sameDir = PipeDirection.Right;
                    oppDir = PipeDirection.Left;
                    break;
                case CardinalDirection.West:
                    sameDir = PipeDirection.Left;
                    oppDir = PipeDirection.Right;
                    break;
                default:
                    throw new ArgumentException("Invalid Direction.");
            }
        }
    }

    public enum CardinalDirection
    {
        North = Direction.North,
        South = Direction.South,
        East = Direction.East,
        West = Direction.West,
    }

    public enum PipeDirection
    {
        None  = 0,

        //Half of a pipe in a direction
        Up    = 1 << 0,
        Down  = 1 << 1,
        Left  = 1 << 2,
        Right = 1 << 3,

        //Straight pipes
        Vertical = Up | Down,
        Horizontial = Left | Right,

        //Bends
        ULBend = Up | Left,
        URBend = Up | Right,
        DLBend = Down | Left,
        DRBend = Down | Right,

        //T-Junctions
        TUp = Up | Horizontial,
        TDown = Down | Horizontial,
        TLeft = Left | Vertical,
        TRight = Right | Vertical,

        //Four way
        All = Up | Down | Left | Right,
    }
}
