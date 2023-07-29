using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Generation.Teg;

[NodeGroup(NodeGroupID.Teg)]
public sealed class TegNodeGroup : BaseNodeGroup
{

}

[DataDefinition]
public sealed class TegNodeGenerator : Node
{
    public override IEnumerable<Node> GetReachableNodes(
        TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        var dir = xform.LocalRotation.GetDir();
        var a = FindCirculator(dir);
        var b = FindCirculator(dir.GetOpposite());

        if (a != null)
            yield return a;

        if (b != null)
            yield return b;

        TegNodeCirculator? FindCirculator(Direction searchDir)
        {
            var targetIdx = gridIndex.Offset(searchDir);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, targetIdx))
            {
                if (node is not TegNodeCirculator circulator)
                    continue;

                var entity = node.Owner;
                var entityXform = xformQuery.GetComponent(entity);
                var entityDir = entityXform.LocalRotation.GetDir();

                if (entityDir == searchDir.GetClockwise90Degrees())
                    return circulator;
            }

            return null;
        }
    }
}

[DataDefinition]
public sealed class TegNodeCirculator : Node
{
    public override IEnumerable<Node> GetReachableNodes(
        TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        var dir = xform.LocalRotation.GetDir();
        var searchDir = dir.GetClockwise90Degrees();
        var targetIdx = gridIndex.Offset(searchDir);

        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, targetIdx))
        {
            if (node is not TegNodeGenerator generator)
                continue;

            var entity = node.Owner;
            var entityXform = xformQuery.GetComponent(entity);
            var entityDir = entityXform.LocalRotation.GetDir();

            if (entityDir == searchDir || entityDir == searchDir.GetOpposite())
            {
                yield return generator;
                break;
            }
        }
    }
}
