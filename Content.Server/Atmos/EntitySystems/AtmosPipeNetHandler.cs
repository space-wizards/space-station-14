using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Systems;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosPipeNetHandler : SingleNodeGroupHandler<PipeNet>
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;

    protected override NodeGroupID NodeGroupID => NodeGroupID.Pipe;

    protected override void InitializeGroup(PipeNet group, Node sourceNode)
    {
        base.InitializeGroup(group, sourceNode);
        group.Grid = Transform(sourceNode.Owner).GridUid;

        if (group.Grid == null)
        {
            // This is probably due to a canister or something like that being spawned in space.
            return;
        }

        _atmosphereSystem.AddPipeNet(group.Grid.Value, group);
    }

    public void UpdateGroup(PipeNet group)
    {
        _atmosphereSystem.React(group.Air, group);
    }

    protected override void LoadNodes(PipeNet group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        foreach (var node in groupNodes)
        {
            var pipeNode = (PipeNode) node;
            group.Air.Volume += pipeNode.Volume;
        }
    }

    protected override void RemoveNode(PipeNet group, Node node)
    {
        base.RemoveNode(group, node);
        // if the node is simply being removed into a separate group, we do nothing, as gas redistribution will be
        // handled by AfterRemake(). But if it is being deleted, we actually want to remove the gas stored in this node.
        if (!node.Deleting || node is not PipeNode pipe)
            return;

        group.Air.Multiply(1f - pipe.Volume / group.Air.Volume);
        group.Air.Volume -= pipe.Volume;
    }

    protected override void AfterRemake(PipeNet group, IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        base.AfterRemake(group, newGroups);
        RemoveFromGridAtmos(group);
        var newAir = new List<GasMixture>(newGroups.Count());
        foreach (var newGroup in newGroups)
        {
            if (newGroup.Key is PipeNet newPipeNet)
                newAir.Add(newPipeNet.Air);
        }

        _atmosphereSystem.DivideInto(group.Air, newAir);
    }

    private void RemoveFromGridAtmos(PipeNet group)
    {
        if (group.Grid == null)
            return;

        _atmosphereSystem.RemovePipeNet(group.Grid.Value, group);
    }

    protected override string GetDebugData(PipeNet group)
    {
        return @$"Pressure: {group.Air.Pressure:G3}
Temperature: {group.Air.Temperature:G3}
Volume: {group.Air.Volume:G3}";
    }
}
