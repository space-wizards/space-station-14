using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Disposal.Unit;
using Content.Shared.NodeContainer;

namespace Content.Server.VentCrawl;

/// <summary>
/// Provides breathing-location overrides for entities that are currently vent-crawling.
/// Reuses <see cref="BeingDisposedComponent"/> to avoid duplicating the "in-pipe" state.
/// </summary>
public sealed partial class BeingVentCrawlSystem : EntitySystem
{
    [Dependency] private NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeContainerComponent, GetBeingDisposedGasEvent>(OnGetBeingDisposedGas);
    }

    private void OnGetBeingDisposedGas(Entity<NodeContainerComponent> ent, ref GetBeingDisposedGasEvent args)
    {
        foreach (var nodeContainerNode in ent.Comp.Nodes)
        {
            if (!_nodeContainer.TryGetNode(ent.Comp, nodeContainerNode.Key, out PipeNode? pipe))
                continue;

            args.Gas = pipe.Air;
            return;
        }
    }
}
