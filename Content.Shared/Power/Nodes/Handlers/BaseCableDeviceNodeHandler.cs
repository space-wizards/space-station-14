using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Nodes.Handlers;

/// <summary>
///     Type of node that connects to a <see cref="CableNode"/> below it.
/// </summary>
public abstract class BaseCableDeviceNodeHandler<T> : NodeHandler<T> where T : CableDeviceNode
{
    protected override bool Connectable(T node)
    {
        return node.Enabled && base.Connectable(node);
    }

    protected override IEnumerable<Node> GetReachableNodes(
        T node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        foreach (var tileNode in GetNodesInTile(gridEnt, gridIndex))
        {
            if (tileNode is CableNode)
                yield return tileNode;
        }
    }
}
