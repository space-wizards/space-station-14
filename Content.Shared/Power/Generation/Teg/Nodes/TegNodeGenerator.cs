using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Generation.Teg.Nodes;

/// <summary>
/// Node used by the central TEG generator component.
/// </summary>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegGeneratorComponent"/>
[DataDefinition]
public sealed partial class TegNodeGenerator : Node
{
    public override IEnumerable<Node> GetReachableNodes(
        Entity<TransformComponent> xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        Entity<MapGridComponent>? grid,
        IEntityManager entMan)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var mapSystem = entMan.System<SharedMapSystem>();
        var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        var dir = xform.Comp.LocalRotation.GetDir();
        var a = FindCirculator(dir);
        var b = FindCirculator(dir.GetOpposite());

        if (a != null)
            yield return a;

        if (b != null)
            yield return b;

        TegNodeCirculator? FindCirculator(Direction searchDir)
        {
            var targetIdx = gridIndex.Offset(searchDir);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, targetIdx, mapSystem))
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
