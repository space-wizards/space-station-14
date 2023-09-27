using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

[Serializable, NetSerializable]
public sealed class NavMapAddNodesMessage : EntityEventArgs
{
    public NetEntity GridUid;
    public List<NetCoordinates> HVNodes = new();
    public List<NetCoordinates> MVNodes = new();
    public List<NetCoordinates> LVNodes = new();

    public NavMapAddNodesMessage(NetEntity gridUid, List<NetCoordinates> hvNodes, List<NetCoordinates> mvNodes, List<NetCoordinates> lvNodes)
    {
        GridUid = gridUid;
        HVNodes = hvNodes;
        MVNodes = mvNodes;
        LVNodes = lvNodes;
    }
}

[Serializable, NetSerializable]
public sealed class NavMapRemoveNodesMessage : EntityEventArgs
{
    public NetEntity GridUid;
    public List<NetCoordinates> HVNodes = new();
    public List<NetCoordinates> MVNodes = new();
    public List<NetCoordinates> LVNodes = new();

    public NavMapRemoveNodesMessage(NetEntity gridUid, List<NetCoordinates> hvNodes, List<NetCoordinates> mvNodes, List<NetCoordinates> lvNodes)
    {
        GridUid = gridUid;
        HVNodes = hvNodes;
        MVNodes = mvNodes;
        LVNodes = lvNodes;
    }
}
