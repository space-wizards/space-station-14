using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.NodeContainer;
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

    /// <inheritdoc/>
    public override void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, AtmosPipeLayer layer, EntityUid? user = null, EntityUid? used = null)
    {
        if (ent.Comp.PipeLayersLocked)
            return;

        base.SetPipeLayer(ent, layer, user, used);

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

        // If a user wasn't responsible for unanchoring the pipe, leave it be
        if (user == null || used == null)
            return;

        // Unanchor the pipe if its new layer overlaps with another pipe
        var xform = Transform(ent);

        if (!HasComp<PipeRestrictOverlapComponent>(ent) || !_pipeRestrictOverlap.CheckOverlap((ent, nodeContainer, xform)))
            return;

        RaiseLocalEvent(ent, new BeforeUnanchoredEvent(user.Value, used.Value));
        _xform.Unanchor(ent, xform);
        RaiseLocalEvent(ent, new UserUnanchoredEvent(user.Value, used.Value));

        _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent)), ent, user.Value);
    }
}
