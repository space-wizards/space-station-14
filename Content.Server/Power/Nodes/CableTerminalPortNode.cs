using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public sealed partial class CableTerminalPortNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(Entity<TransformComponent> xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            Entity<MapGridComponent>? grid,
            IEntityManager entMan)
        {
            if (!xform.Comp.Anchored || grid is not { } gridEnt)
                yield break;

            var mapSystem = entMan.System<SharedMapSystem>();
            var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

            var nodes = NodeHelpers.GetCardinalNeighborNodes(nodeQuery, gridEnt, gridIndex, mapSystem, includeSameTile: false);
            foreach (var (dir, node) in nodes)
            {
                if (node is CableTerminalNode
                    && dir != Direction.Invalid
                    && xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir().GetOpposite() == dir)
                    yield return node;
            }
        }
    }
}
