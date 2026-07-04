namespace Content.Shared.NodeContainer;

public interface INodeGroupManager
{
    void Register(NodeGroupPrototype groupProto);

    NodeGroupPrototype this[int id] { get; }

    NodeGroupPrototype this[string id] { get; }

    int Count { get; }
}

/// <summary>
/// A manager for <see cref="NodeGroupPrototype"/>'s numeric ID.
/// </summary>
public sealed class NodeGroupManager : INodeGroupManager
{
    private readonly List<NodeGroupPrototype> _groupDefs = new();
    private readonly Dictionary<string, NodeGroupPrototype> _groupNames = new();

    public void Register(NodeGroupPrototype groupProto)
    {
        var id = checked((ushort) (_groupDefs.Count + 1));
        groupProto.AssignGroupId(id);
        _groupDefs.Add(groupProto);
        _groupNames.Add(groupProto.ID, groupProto);
    }

    public NodeGroupPrototype this[int id] => _groupDefs[id];

    public NodeGroupPrototype this[string id] => _groupNames[id];

    public int Count => _groupDefs.Count;
}
