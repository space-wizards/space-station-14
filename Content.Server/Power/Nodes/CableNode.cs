using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public sealed partial class CableNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(
            EntityUid uid,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MapGridComponent> gridQuery,
            IEntityManager entMan,
            SharedMapSystem mapSystem)
        {
            if (!xformQuery.TryGetComponent(uid, out var xform)
                || !xform.Anchored
                || xform.GridUid == null)
                yield break;

            if (!gridQuery.TryGetComponent(xform.GridUid.Value, out var grid))
                yield break;

            var gridIndex = mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

            // While we go over adjacent nodes, we build a list of blocked directions due to
            // incoming or outgoing wire terminals.
            var terminalDirs = 0;
            List<(Direction, Node)> nodeDirs = new();

            foreach (var (dir, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, xform, grid, gridIndex, mapSystem))
            {
                if (node is CableNode && node != this)
                {
                    nodeDirs.Add((dir, node));
                }

                if (node is CableDeviceNode && dir == Direction.Invalid)
                {
                    // device on same tile
                    nodeDirs.Add((Direction.Invalid, node));
                }

                if (node is CableTerminalNode)
                {
                    if (dir == Direction.Invalid)
                    {
                        // On own tile, block direction it faces
                        terminalDirs |= 1 << (int) xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir();
                    }
                    else
                    {
                        var terminalDir = xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir();
                        if (terminalDir.GetOpposite() == dir)
                        {
                            // Target tile has a terminal towards us, block the direction.
                            terminalDirs |= 1 << (int) dir;
                        }
                    }
                }
            }

            foreach (var (dir, node) in nodeDirs)
            {
                // If there is a wire terminal connecting across this direction, skip the node.
                if (dir != Direction.Invalid && (terminalDirs & (1 << (int) dir)) != 0)
                    continue;

                yield return node;
            }
        }
    }
}
