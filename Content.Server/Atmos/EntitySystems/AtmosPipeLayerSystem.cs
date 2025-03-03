using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosPipeLayerSystem : SharedAtmosPipeLayerSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    public override void SetPipeLayer(Entity<AtmosPipeLayerComponent> ent, int layer)
    {
        base.SetPipeLayer(ent, layer);

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        foreach (var (id, node) in nodeContainer.Nodes)
        {
            if (node is not PipeNode { } pipeNode)
                continue;

            pipeNode.CurrentPipeLayer = ent.Comp.CurrentPipeLayer;

            if (pipeNode.NodeGroup != null)
                _nodeGroup.QueueRemakeGroup((BaseNodeGroup)pipeNode.NodeGroup);
        }
    }
}
