using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Nodes.Handlers;

public sealed partial class CableTerminalNodeHandler : BaseCableDeviceNodeHandler<CableTerminalNode>
{
    protected override IEnumerable<Node> GetReachableNodes(
        CableTerminalNode node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        var dir = xform.Comp.LocalRotation.GetDir();
        var targetIdx = gridIndex.Offset(dir);

        foreach (var tileNode in GetNodesInTile(gridEnt, targetIdx))
        {
            if (tileNode is CableTerminalPortNode)
                yield return tileNode;
        }

        foreach (var reachableNode in base.GetReachableNodes(node, xform, grid))
        {
            yield return reachableNode;
        }
    }
}
