using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    [Node("DirectionalNode")]
    public class DirectionalNode : Node
    {
        private Connection _connectionDirection = Connection.Vertical;

        protected override IEnumerable<Node> GetReachableNodes()
        {
            foreach (CardinalDirection direction in Enum.GetValues(typeof(CardinalDirection)))
            {
                ConnectionFromCardinal(direction, out var ownNeededConnection, out var otherDirToCheck);
                if ((ownNeededConnection & _connectionDirection) == Connection.None)
                {
                    yield break;
                }
                var directionalNodesInDirection = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir((Direction) direction)
                    .Select(entity => entity.TryGetComponent<NodeContainerComponent>(out var container) ? container : null)
                    .Where(container => container != null)
                    .SelectMany(container => container.Nodes)
                    .OfType<DirectionalNode>()
                    .Where(node => node != null && node != this);
                foreach (var pipe in directionalNodesInDirection)
                {
                    if ((pipe._connectionDirection & otherDirToCheck) != Connection.None)
                    {
                        yield return pipe;
                    }
                }
            }
        }

        private void ConnectionFromCardinal(CardinalDirection direction, out Connection sameDir, out Connection oppDir)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    sameDir = Connection.Up;
                    oppDir = Connection.Down;
                    break;
                case CardinalDirection.South:
                    sameDir = Connection.Down;
                    oppDir = Connection.Up;
                    break;
                case CardinalDirection.East:
                    sameDir = Connection.Right;
                    oppDir = Connection.Left;
                    break;
                case CardinalDirection.West:
                    sameDir = Connection.Left;
                    oppDir = Connection.Right;
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

    public enum Connection
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
        A = Up | Left,
        B = Up | Right,
        C = Down | Left,
        D = Down | Right,

        //T-Junctions
        TUp = Up | Horizontial,
        TDown = Down | Horizontial,
        TLeft = Vertical | Left,
        TRight = Vertical | Right,

        //Four way
        All = Up | Down | Left | Right,
    }
}
