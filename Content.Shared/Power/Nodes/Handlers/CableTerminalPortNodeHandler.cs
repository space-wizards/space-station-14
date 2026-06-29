using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Nodes.Handlers;

public sealed partial class CableTerminalPortNodeHandler : NodeHandler<CableTerminalPortNode>
{
    protected override IEnumerable<Node> GetReachableNodes(
        CableTerminalPortNode node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        var nodes = GetCardinalNeighborNodes(gridEnt, gridIndex, includeSameTile: false);
        foreach (var (dir, nearNode) in nodes)
        {
            if (nearNode is CableTerminalNode
                && dir != Direction.Invalid
                && Transform(nearNode.Owner).LocalRotation.GetCardinalDir().GetOpposite() == dir)
                yield return nearNode;
        }
    }
}
