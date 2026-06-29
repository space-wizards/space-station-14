using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Shared.Atmos.Nodes.Handlers;

public sealed class PortablePipeNodeHandler : BasePipeNodeHandler<PortablePipeNode>
{
    protected override IEnumerable<Node> GetReachableNodes(
        PortablePipeNode node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        foreach (var tileNode in GetNodesInTile(gridEnt, gridIndex))
        {
            if (tileNode is PortPipeNode)
                yield return tileNode;
        }

        foreach (var reachableNode in base.GetReachableNodes(node, xform, grid))
        {
            yield return reachableNode;
        }
    }
}
