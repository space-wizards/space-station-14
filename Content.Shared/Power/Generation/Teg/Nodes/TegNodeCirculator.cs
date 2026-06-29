using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Shared.Power.Generation.Teg.Nodes;

/// <summary>
/// Node used by the central TEG circulator entities.
/// </summary>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegCirculatorComponent"/>
[DataDefinition]
public sealed partial class TegNodeCirculator : Node
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
        var searchDir = dir.GetClockwise90Degrees();
        var targetIdx = gridIndex.Offset(searchDir);

        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, targetIdx, mapSystem))
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
