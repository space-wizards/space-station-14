using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.NodeContainer.Nodes.Handlers;

public sealed class AdjacentNodeHandler : NodeHandler<AdjacentNode>
{
    protected override IEnumerable<Node> GetReachableNodes(AdjacentNode node, Entity<TransformComponent> xform, Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        foreach (var (_, near) in GetCardinalNeighborNodes(gridEnt, gridIndex))
        {
            if (near != node)
                yield return near;
        }
    }
}
