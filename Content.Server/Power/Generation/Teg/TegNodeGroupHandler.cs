using System.Linq;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Generation.Teg.Nodes;

namespace Content.Server.Power.Generation.Teg;

public sealed partial class TegNodeGroupHandler : SingleNodeGroupHandler<TegNodeGroup>
{
    [Dependency] private TegSystem _tegSystem = default!;

    protected override NodeGroupID NodeGroupID => NodeGroupID.Teg;

    protected override void LoadNodes(TegNodeGroup group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);

        if (groupNodes.Count > 3)
        {
            // Somehow got more TEG parts. Probably shenanigans. Bail.
            return;
        }

        group.Generator = groupNodes.OfType<TegNodeGenerator>().SingleOrDefault();
        if (group.Generator != null)
        {
            // If we have a generator, we can assign CirculatorA and CirculatorB based on relative rotation.
            var xformGenerator = Transform(group.Generator.Owner);
            var genDir = xformGenerator.LocalRotation.GetDir();

            foreach (var node in groupNodes)
            {
                if (node is not TegNodeCirculator circulator)
                    continue;

                var xform = Transform(node.Owner);
                var dir = xform.LocalRotation.GetDir();
                if (genDir.GetClockwise90Degrees() == dir)
                {
                    group.CirculatorA = circulator;
                }
                else
                {
                    group.CirculatorB = circulator;
                }
            }

        }

        group.IsFullyBuilt = group.Generator != null && group.CirculatorA != null && group.CirculatorB != null;

        foreach (var node in groupNodes)
        {
            switch (node)
            {
                case TegNodeGenerator generator:
                    _tegSystem.UpdateGeneratorConnectivity(generator.Owner, group);
                    break;
                case TegNodeCirculator circulator:
                    _tegSystem.UpdateCirculatorConnectivity(circulator.Owner, group);
                    break;
            }
        }
    }
}
