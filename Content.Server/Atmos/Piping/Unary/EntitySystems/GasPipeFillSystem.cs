using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

public sealed class GasPipeFillSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeFillComponent, NodeGroupsRebuilt>(OnNodeUpdate);
    }

    private void OnNodeUpdate(EntityUid uid, PipeFillComponent comp, ref NodeGroupsRebuilt args)
    {
        if (_nodeContainer.TryGetNode(uid, comp.NodeName, out PipeNode? tank) && tank.NodeGroup is PipeNet net)
        {
            _atmos.Merge(net.Air, comp.Air);
        }

        RemComp<PipeFillComponent>(uid); // only fire once, and fail dumb.

    }
}
