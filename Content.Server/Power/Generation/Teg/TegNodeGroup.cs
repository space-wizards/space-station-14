using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// Node group that connects the central TEG with its two circulators.
/// </summary>
/// <seealso cref="TegNodeGenerator"/>
/// <seealso cref="TegNodeCirculator"/>
/// <seealso cref="TegSystem"/>
[NodeGroup(NodeGroupID.Teg)]
public sealed class TegNodeGroup : BaseNodeGroup
{
    /// <summary>
    /// If true, this TEG is fully built and has all its parts properly connected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsFullyBuilt { get; private set; }

    /// <summary>
    /// The central generator component.
    /// </summary>
    /// <seealso cref="TegGeneratorComponent"/>
    [ViewVariables(VVAccess.ReadWrite)]
    public TegNodeGenerator? Generator { get; private set; }

    // Illustration for how the TEG A/B circulators are laid out.
    // Circulator B       Generator        Circulator A
    //     ^                   ->               |
    //     |                                    V
    // They have rotations like the arrows point out.

    /// <summary>
    /// The A-side circulator. This is the circulator that is in the direction FACING the center component's rotation.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    /// <seealso cref="TegCirculatorComponent"/>
    [ViewVariables(VVAccess.ReadWrite)]
    public TegNodeCirculator? CirculatorA { get; private set; }

    /// <summary>
    /// The B-side circulator. This circulator is opposite <see cref="CirculatorA"/>.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    /// <seealso cref="TegCirculatorComponent"/>
    [ViewVariables(VVAccess.ReadWrite)]
    public TegNodeCirculator? CirculatorB { get; private set; }

    private IEntityManager? _entityManager;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _entityManager = entMan;
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        DebugTools.Assert(groupNodes.Count <= 3, "The TEG has at most 3 parts");
        DebugTools.Assert(_entityManager != null);

        base.LoadNodes(groupNodes);

        Generator = groupNodes.OfType<TegNodeGenerator>().SingleOrDefault();
        if (Generator != null)
        {
            // If we have a generator, we can assign CirculatorA and CirculatorB based on relative rotation.
            var xformGenerator = _entityManager.GetComponent<TransformComponent>(Generator.Owner);
            var genDir = xformGenerator.LocalRotation.GetDir();

            foreach (var node in groupNodes)
            {
                if (node is not TegNodeCirculator circulator)
                    continue;

                var xform = _entityManager.GetComponent<TransformComponent>(node.Owner);
                var dir = xform.LocalRotation.GetDir();
                if (genDir.GetClockwise90Degrees() == dir)
                {
                    CirculatorA = circulator;
                }
                else
                {
                    CirculatorB = circulator;
                }
            }

        }

        IsFullyBuilt = Generator != null && CirculatorA != null && CirculatorB != null;

        var tegSystem = _entityManager.EntitySysManager.GetEntitySystem<TegSystem>();
        foreach (var node in groupNodes)
        {
            if (node is TegNodeGenerator generator)
                tegSystem.UpdateGeneratorConnectivity(generator.Owner, this);

            if (node is TegNodeCirculator circulator)
                tegSystem.UpdateCirculatorConnectivity(circulator.Owner, this);
        }
    }
}

/// <summary>
/// Node used by the central TEG generator component.
/// </summary>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegGeneratorComponent"/>
[DataDefinition]
public sealed partial class TegNodeGenerator : Node
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

/// <summary>
/// Node used by the central TEG circulator entities.
/// </summary>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegCirculatorComponent"/>
[DataDefinition]
public sealed partial class TegNodeCirculator : Node
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
