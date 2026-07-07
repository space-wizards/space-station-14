using System.Collections.Frozen;
using Content.Shared.NodeContainer.Components;
using Content.Shared.NodeContainer.Systems;

namespace Content.Shared.NodeContainer;

public interface INodeGroupManager
{
    void Initialize();

    void Register(NodeGroupPrototype groupProto);

    NodeGroupPrototype this[int id] { get; }

    NodeGroupPrototype this[string id] { get; }

    int Count { get; }

    void RegisterGroup(ushort groupId, Type compType);

    void RegisterGroupHandler(Type compType, INodeGroupHandler handler);

    void RegisterNodeHandler(Type nodeType, INodeHandler handler);

    Type GetNodeGroupComponentType(ushort groupId);

    INodeGroupHandler GetNodeGroupHandler(Type groupType);

    INodeGroupHandler GetNodeGroupHandler(IComponent group);

    INodeGroupHandler GetNodeGroupHandler(ushort groupId);

    INodeHandler GetNodeHandler(Type nodeType);

    INodeHandler GetNodeHandler(Node node);
}

/// <summary>
/// A manager for <see cref="NodeGroupPrototype"/>'s numeric ID.
/// </summary>
public sealed class NodeGroupManager : INodeGroupManager
{
    private readonly List<NodeGroupPrototype> _groupDefs = new();
    private readonly Dictionary<string, NodeGroupPrototype> _groupNames = new();

    /// <summary>
    /// An array that associates each <see cref="NodeGroupPrototype"/> numeric ID with a node group specific component type.
    /// </summary>
    private Type[] _nodeGroupTypes = Array.Empty<Type>();

    /// <summary>
    /// A dictionary of <see cref="INodeGroupHandler"/>s that handle <see cref="NodeGroupComponent"/>s with a specific Node group component Type.
    /// </summary>
    private readonly Dictionary<Type, INodeGroupHandler> _nodeGroupHandlers = new();

    private FrozenDictionary<Type, INodeGroupHandler> _frozenNodeGroupHandlers = default!;

    /// <summary>
    /// A dictionary of <see cref="Node"/> Types and <see cref="INodeHandler"/>s.
    /// </summary>
    private readonly Dictionary<Type, INodeHandler> _nodeHandlers = new();

    private FrozenDictionary<Type, INodeHandler> _frozenNodeHandlers = default!;

    public void Initialize()
    {
        Array.Resize(ref _nodeGroupTypes, _groupDefs.Count);

        foreach (var handler in _nodeGroupHandlers.Values)
        {
            handler.RegisterGroups();
        }

        _frozenNodeGroupHandlers = _nodeGroupHandlers.ToFrozenDictionary();
        _frozenNodeHandlers = _nodeHandlers.ToFrozenDictionary();
    }

    public void Register(NodeGroupPrototype groupProto)
    {
        var id = checked((ushort) _groupDefs.Count);
        groupProto.AssignGroupId(id);
        _groupDefs.Add(groupProto);
        _groupNames.Add(groupProto.ID, groupProto);
    }

    public NodeGroupPrototype this[int id] => _groupDefs[id];

    public NodeGroupPrototype this[string id] => _groupNames[id];

    public int Count => _groupDefs.Count;

    public void RegisterGroup(ushort groupId, Type compType)
    {
        _nodeGroupTypes[groupId] = compType;
    }

    public void RegisterGroupHandler(Type compType, INodeGroupHandler handler)
    {
        _nodeGroupHandlers.Add(compType, handler);
    }

    public void RegisterNodeHandler(Type nodeType, INodeHandler handler)
    {
        _nodeHandlers.Add(nodeType, handler);
    }

    public Type GetNodeGroupComponentType(ushort groupId) => _nodeGroupTypes[groupId];

    public INodeGroupHandler GetNodeGroupHandler(Type groupType)
    {
        return _frozenNodeGroupHandlers[groupType];
    }

    public INodeGroupHandler GetNodeGroupHandler(IComponent group)
    {
        return GetNodeGroupHandler(group.GetType());
    }

    public INodeGroupHandler GetNodeGroupHandler(ushort groupId)
    {
        return GetNodeGroupHandler(_nodeGroupTypes[groupId]);
    }

    public INodeHandler GetNodeHandler(Type nodeType)
    {
        return _frozenNodeHandlers[nodeType];
    }

    public INodeHandler GetNodeHandler(Node node)
    {
        return GetNodeHandler(node.GetType());
    }
}
