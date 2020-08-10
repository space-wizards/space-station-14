using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeNode.PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    public class PipeNode : Node
    {
        [ViewVariables]
        public PipeDirection PipeDirection { get => _pipeDirection; set => SetPipeDirection(value); }
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
                if ((_pipeDirection & ownNeededConnection) == PipeDirection.None)
                {
                    continue;
                }
                var pipeNodesInDirection = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir((Direction) direction)
                    .Select(entity => entity.TryGetComponent<NodeContainerComponent>(out var container) ? container : null)
                    .Where(container => container != null)
                    .SelectMany(container => container.Nodes)
                    .OfType<PipeNode>()
                    .Where(pipeNode => (pipeNode._pipeDirection & theirNeededConnection) != PipeDirection.None);
                foreach (var pipeNode in pipeNodesInDirection)
                {
                    yield return pipeNode;
                }
            }
        }

        private void PipeDirectionFromCardinal(CardinalDirection direction, out PipeDirection sameDir, out PipeDirection oppDir)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    sameDir = PipeDirection.North;
                    oppDir = PipeDirection.South;
                    break;
                case CardinalDirection.South:
                    sameDir = PipeDirection.South;
                    oppDir = PipeDirection.North;
                    break;
                case CardinalDirection.East:
                    sameDir = PipeDirection.East;
                    oppDir = PipeDirection.West;
                    break;
                case CardinalDirection.West:
                    sameDir = PipeDirection.West;
                    oppDir = PipeDirection.East;
                    break;
                default:
                    throw new ArgumentException("Invalid Direction.");
            }
        }

        private void SetPipeDirection(PipeDirection pipeDirection)
        {
            throw new NotImplementedException();

            NodeGroup.RemoveNode(this);
            ClearNodeGroup();
            _pipeDirection = pipeDirection;
            TryAssignGroupIfNeeded();
            //CombineGroupWithReachable();
        }

        private enum CardinalDirection
        {
            North = Direction.North,
            South = Direction.South,
            East = Direction.East,
            West = Direction.West,
        }
    }

    public enum PipeDirection
    {
        None = 0,

        //Half of a pipe in a direction
        North = 1 << 0,
        South = 1 << 1,
        West = 1 << 2,
        East = 1 << 3,

        //Straight pipes
        Longitudinal = North | South,
        Lateral = West | East,

        //Bends
        NWBend = North | West,
        NEBend = North | East,
        SWBend = South | West,
        SEBend = South | East,

        //T-Junctions
        TNorth = North | Lateral,
        TSouth = South | Lateral,
        TWest = West | Longitudinal,
        TEast = East | Longitudinal,

        //Four way
        FourWay = North | South | East | West,

        All = -1,
    }
}
