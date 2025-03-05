using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AtmosPipeLayersComponent> ent, ref ComponentInit args)
    {
        SetPipeLayer(ent, ent.Comp.CurrentPipeLayer);
    }

    public override void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, int layer)
    {
        base.SetPipeLayer(ent, layer);

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        foreach (var (id, node) in nodeContainer.Nodes)
        {
            if (node is not PipeNode { } pipeNode)
                continue;

            if (pipeNode.CurrentPipeLayer == ent.Comp.CurrentPipeLayer)
                continue;

            pipeNode.CurrentPipeLayer = ent.Comp.CurrentPipeLayer;

            if (pipeNode.NodeGroup != null)
                _nodeGroup.QueueRemakeGroup((BaseNodeGroup)pipeNode.NodeGroup);
        }
    }
}
