using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Nodes.Handlers;

public sealed partial class CableNodeHandler : NodeHandler<CableNode>
{
    protected override IEnumerable<Node> GetReachableNodes(
        CableNode node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        // While we go over adjacent nodes, we build a list of blocked directions due to
        // incoming or outgoing wire terminals.
        var terminalDirs = 0;
        List<(Direction, Node)> nodeDirs = new();

        foreach (var (dir, nearNode) in GetCardinalNeighborNodes(gridEnt, gridIndex))
        {
            if (nearNode is CableNode && nearNode != node)
            {
                nodeDirs.Add((dir, nearNode));
            }

            if (nearNode is CableDeviceNode && dir == Direction.Invalid)
            {
                // device on same tile
                nodeDirs.Add((Direction.Invalid, nearNode));
            }

            if (nearNode is CableTerminalNode)
            {
                if (dir == Direction.Invalid)
                {
                    // On own tile, block direction it faces
                    terminalDirs |= 1 << (int) Transform(nearNode.Owner).LocalRotation.GetCardinalDir();
                }
                else
                {
                    var terminalDir = Transform(nearNode.Owner).LocalRotation.GetCardinalDir();
                    if (terminalDir.GetOpposite() == dir)
                    {
                        // Target tile has a terminal towards us, block the direction.
                        terminalDirs |= 1 << (int) dir;
                    }
                }
            }
        }

        foreach (var (dir, foundNode) in nodeDirs)
        {
            // If there is a wire terminal connecting across this direction, skip the node.
            if (dir != Direction.Invalid && (terminalDirs & (1 << (int) dir)) != 0)
                continue;

            yield return foundNode;
        }
    }
}
