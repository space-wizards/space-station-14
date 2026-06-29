namespace Content.Shared.NodeContainer.NodeGroups;

[Virtual]
public class BaseNodeGroup : INodeGroup
{
    public bool Remaking { get; set; }

    /// <summary>
    ///     The list of nodes in this group.
    /// </summary>
    [ViewVariables]
    public List<Node> Nodes { get; } = new();

    [ViewVariables]
    public int NodeCount => Nodes.Count;

    /// <summary>
    ///     Debug variable to indicate that this NodeGroup should not be being used by anything.
    /// </summary>
    [ViewVariables]
    public bool Removed { get; set; } = false;

    /// <summary>
    ///     Network ID of this group for client-side debug visualization of nodes.
    /// </summary>
    [ViewVariables]
    public int NetId;

    [ViewVariables]
    public NodeGroupID GroupId { get; set; }
}
