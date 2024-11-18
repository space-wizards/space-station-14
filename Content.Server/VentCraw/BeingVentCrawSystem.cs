using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.VentCraw.Components;

namespace Content.Server.VentCraw;

public sealed class BeingVentCrawSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<BeingVentCrawComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<BeingVentCrawComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnGetAir(EntityUid uid, BeingVentCrawComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            args.Handled = true;
            return;
        }
    }

    private void OnInhaleLocation(EntityUid uid, BeingVentCrawComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            return;
        }
    }

    private void OnExhaleLocation(EntityUid uid, BeingVentCrawComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            return;
        }
    }
}
