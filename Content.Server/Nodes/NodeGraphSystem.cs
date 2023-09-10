using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Nodes;

public sealed partial class NodeGraphSystem : SharedNodeGraphSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeGraphComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    /// <remarks>
    /// Since graphs should never be naturally deleted except by being emptied of nodes this should be unreachable unless badmins manually delete one.
    /// In that case we don't want there to be any nodes left with a null graph so yeet all the nodes too.
    /// </remarks>
    private void OnEntityTerminating(EntityUid uid, NodeGraphComponent comp, ref EntityTerminatingEvent args)
    {
        if (comp.Nodes.Count <= 0)
            return;

        Log.Debug($"Node graph {ToPrettyString(uid)} deleted while still containing nodes; we are yeeting the __entire__ graph.");
        while (comp.Nodes.FirstOrNull() is { } nodeId)
        {
            Del(nodeId);
        }
    }
}
