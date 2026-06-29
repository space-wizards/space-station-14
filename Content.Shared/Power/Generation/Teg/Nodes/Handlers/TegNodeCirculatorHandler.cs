using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Generation.Teg.Nodes.Handlers;

public sealed class TegNodeCirculatorHandler : NodeHandler<TegNodeCirculator>
{
    protected override IEnumerable<Node> GetReachableNodes(
        TegNodeCirculator node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        var dir = xform.Comp.LocalRotation.GetDir();
        var searchDir = dir.GetClockwise90Degrees();
        var targetIdx = gridIndex.Offset(searchDir);

        foreach (var tileNode in GetNodesInTile(gridEnt, targetIdx))
        {
            if (tileNode is not TegNodeGenerator generator)
                continue;

            var entity = tileNode.Owner;
            var entityXform = Transform(entity);
            var entityDir = entityXform.LocalRotation.GetDir();

            if (entityDir == searchDir || entityDir == searchDir.GetOpposite())
            {
                yield return generator;
                break;
            }
        }
    }
}
