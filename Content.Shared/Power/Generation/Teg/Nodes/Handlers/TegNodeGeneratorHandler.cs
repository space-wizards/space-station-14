using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Generation.Teg.Nodes.Handlers;

public sealed class TegNodeGeneratorHandler : NodeHandler<TegNodeGenerator>
{
    protected override IEnumerable<Node> GetReachableNodes(
        TegNodeGenerator node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var gridIndex = MapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        var dir = xform.Comp.LocalRotation.GetDir();
        var a = FindCirculator(dir);
        var b = FindCirculator(dir.GetOpposite());

        if (a != null)
            yield return a;

        if (b != null)
            yield return b;

        yield break;

        TegNodeCirculator? FindCirculator(Direction searchDir)
        {
            var targetIdx = gridIndex.Offset(searchDir);

            foreach (var tileNode in GetNodesInTile(gridEnt, targetIdx))
            {
                if (tileNode is not TegNodeCirculator circulator)
                    continue;

                var entity = tileNode.Owner;
                var entityXform = Transform(entity);
                var entityDir = entityXform.LocalRotation.GetDir();

                if (entityDir == searchDir.GetClockwise90Degrees())
                    return circulator;
            }

            return null;
        }
    }
}
