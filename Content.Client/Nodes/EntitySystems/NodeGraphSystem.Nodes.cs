using Content.Client.Nodes.Components;
using Content.Shared.Nodes;
using Robust.Shared.GameStates;

namespace Content.Client.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    #region Event Handlers

    private void HandleComponentState(EntityUid uid, GraphNodeComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not NodeVisState state)
            return;

        comp.Edges = state.Edges;
        comp.HostId = state.HostId;
        comp.GraphId = state.GraphId;
        comp.GraphProto = state.GraphProto;
    }

    #endregion Event Handlers
}
