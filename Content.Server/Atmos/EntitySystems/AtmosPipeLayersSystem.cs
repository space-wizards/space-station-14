using Content.Server.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Popups;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// The system responsible for checking and adjusting the connection layering of gas pipes
/// </summary>
public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly PipeRestrictOverlapSystem _pipeRestrictOverlap = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AtmosPipeLayersComponent> ent, ref ComponentInit args)
    {
        SetPipeLayer(ent, ent.Comp.CurrentPipeLayer);
    }

    /// <summary>
    /// Sets an entity's pipe layer to a specified value
    /// </summary>
    /// <param name="ent">The pipe entity</param>
    /// <param name="layer"> The new layer value
    /// <param name="user">The player entity who adjusting the pipe layer</param>
    public override void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, int layer, EntityUid? user = null)
    {
        if (ent.Comp.PipeLayersLocked)
            return;

        base.SetPipeLayer(ent, layer);

        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        // Update the layer values of all pipe nodes associated with the entity
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

        // Unanchor the pipe if the new layer overlaps with an existing one
        var xform = Transform(ent);

        if (HasComp<PipeRestrictOverlapComponent>(ent) &&
            _pipeRestrictOverlap.CheckOverlap((ent, nodeContainer, xform)))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent)), ent, user.Value);

            _xform.Unanchor(ent, xform);
        }
    }
}
