using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed partial class GasPipeManifoldSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPipeManifoldComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<GasPipeManifoldComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        foreach (var inletName in ent.Comp.InletNames)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, inletName, out PipeNode? inlet))
                continue;

            foreach (var outletName in ent.Comp.OutletNames)
            {
                if (!_nodeContainer.TryGetNode(nodeContainer, outletName, out PipeNode? outlet))
                    continue;

                inlet.AddAlwaysReachable(outlet);
                outlet.AddAlwaysReachable(inlet);
            }
        }
    }
}
