using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.NodeContainer;

public interface INode
{
    NodeGroupID NodeGroupID { get; set; }

    /// <summary>
    ///     The node group this node is a part of.
    /// </summary>
    INodeGroup? NodeGroup { get; set; }

    /// <summary>
    ///     The entity that owns this node via its <see cref="NodeContainerComponent"/>.
    /// </summary>
    EntityUid Owner { get; set; }

    /// <summary>
    ///     Name of this node on the owning <see cref="NodeContainerComponent"/>.
    /// </summary>
    string Name { get; set; }

    bool NeedAnchored { get; set; }

    /// <summary>
    ///    Prevents a node from being used by other nodes while midway through removal.
    /// </summary>
    bool Deleting { get; set; }

    /// <summary>
    ///     All compatible nodes that are reachable by this node.
    ///     Effectively, active connections out of this node.
    /// </summary>
    HashSet<Node> ReachableNodes { get; set; }

    int FloodGen { get; set; }
    int UndirectGen { get; set; }
    bool FlaggedForFlood { get; set; }
    int NetId { get; set; }
}

