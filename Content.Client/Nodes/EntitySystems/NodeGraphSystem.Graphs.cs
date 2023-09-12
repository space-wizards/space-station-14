using Content.Client.Nodes.Components;
using Content.Shared.Nodes;
using Robust.Shared.GameStates;

namespace Content.Client.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    #region Event Handlers

    private void HandleComponentState(EntityUid uid, NodeGraphComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not GraphVisState state)
            return;

        comp.VisColor = state.Color;
        comp.Size = state.Size;
    }

    #endregion Event Handlers
}
