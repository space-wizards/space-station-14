using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.Map;

namespace Content.Server.Pinpointer;

[RegisterComponent]
public sealed partial class NavMapNodeComponent : Component
{
    public List<EntityUid> QueuedNodesToAdd = new();
    public Dictionary<NodeGroupID, List<EntityCoordinates>> QueuedNodesToRemove = new();
    public int MaxNodesProcessedPerUpdate = 1000;
}
