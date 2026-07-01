using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.NodeContainer;

/// <summary>
///     Organizes themselves into distinct <see cref="INodeGroup"/>s with other <see cref="Node"/>s
///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class Node : INode
{
    /// <summary>
    ///     An ID used as a criteria for combining into groups. Determines which <see cref="INodeGroup"/>
    ///     implementation is used as a group.
    /// </summary>
    [DataField("nodeGroupID")]
    public NodeGroupID NodeGroupID { get; set; } = NodeGroupID.Default;

    /// <summary>
    ///     The node group this node is a part of.
    /// </summary>
    [ViewVariables] public INodeGroup? NodeGroup { get; set; }

    /// <summary>
    ///     The entity that owns this node via its <see cref="NodeContainerComponent"/>.
    /// </summary>
    [ViewVariables] public EntityUid Owner { get; set; }

    /// <summary>
    ///     Name of this node on the owning <see cref="NodeContainerComponent"/>.
    /// </summary>
    public string Name { get; set; }

    [DataField]
    public bool NeedAnchored { get; set; } = true;

    [DataField]
    public bool Examinable { get; set; }

    /// <summary>
    ///    Prevents a node from being used by other nodes while midway through removal.
    /// </summary>
    public bool Deleting { get; set; }

    /// <summary>
    ///     All compatible nodes that are reachable by this node.
    ///     Effectively, active connections out of this node.
    /// </summary>
    public HashSet<Node> ReachableNodes { get; set; } = new();

    public int FloodGen { get; set; }
    public int UndirectGen { get; set; }
    public bool FlaggedForFlood { get; set; }
    public int NetId { get; set; }
}
