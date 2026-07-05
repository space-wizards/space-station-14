using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Components;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosPipeNetHandler : SingleNodeGroupHandler<PipeNetComponent>
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;

    protected override ProtoId<NodeGroupPrototype> NodeGroupID => "Pipe";

    protected override void InitializeGroup(Entity<NodeGroupComponent, PipeNetComponent> group, Node sourceNode)
    {
        base.InitializeGroup(group, sourceNode);
        group.Comp2.Grid = Transform(sourceNode.Owner).GridUid;

        if (group.Comp2.Grid == null)
        {
            // This is probably due to a canister or something like that being spawned in space.
            return;
        }

        _atmosphereSystem.AddPipeNet(group.Comp2.Grid.Value, (group.Owner, group.Comp2));
    }

    protected override void LoadNodes(Entity<NodeGroupComponent, PipeNetComponent> group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        foreach (var node in groupNodes)
        {
            var pipeNode = (PipeNode) node;
            group.Comp2.Air.Volume += pipeNode.Volume;
            pipeNode.PipeNet = (group.Owner, group.Comp2);
        }
    }

    protected override void RemoveNode(Entity<NodeGroupComponent, PipeNetComponent> group, Node node)
    {
        base.RemoveNode(group, node);
        // if the node is simply being removed into a separate group, we do nothing, as gas redistribution will be
        // handled by AfterRemake(). But if it is being deleted, we actually want to remove the gas stored in this node.
        if (!node.Deleting || node is not PipeNode pipe)
            return;

        group.Comp2.Air.Multiply(1f - pipe.Volume / group.Comp2.Air.Volume);
        group.Comp2.Air.Volume -= pipe.Volume;
        pipe.PipeNet = null;
    }

    protected override void AfterRemake(Entity<NodeGroupComponent, PipeNetComponent> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups)
    {
        RemoveFromGridAtmos(group);
        var newAir = new List<GasMixture>(newGroups.Count());
        foreach (var newGroup in newGroups)
        {
            if (Query.TryComp(newGroup.Key, out var newPipeNet))
                newAir.Add(newPipeNet.Air);
        }

        _atmosphereSystem.DivideInto(group.Comp2.Air, newAir);
        base.AfterRemake(group, newGroups);
    }

    public void UpdateGroup(Entity<PipeNetComponent> group)
    {
        _atmosphereSystem.React(group.Comp.Air, group.Comp);
    }

    private void RemoveFromGridAtmos(Entity<NodeGroupComponent, PipeNetComponent> group)
    {
        if (group.Comp2.Grid == null)
            return;

        _atmosphereSystem.RemovePipeNet(group.Comp2.Grid.Value, group);
    }

    protected override string GetDebugData(Entity<NodeGroupComponent, PipeNetComponent> group)
    {
        return $"""
            Pressure: {group.Comp2.Air.Pressure:G3}
            Temperature: {group.Comp2.Air.Temperature:G3}
            Volume: {group.Comp2.Air.Volume:G3}
            """;
    }
}
