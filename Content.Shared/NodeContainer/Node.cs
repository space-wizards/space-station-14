using Content.Shared.NodeContainer.Components;

namespace Content.Shared.NodeContainer;

/// <summary>
///     Organizes themselves into distinct node groups with other <see cref="Node"/>s
///     that they can "reach" and have the same <see cref="Node.NodeGroupID"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class Node : INode
{
    [DataField]
    public NodeGroupID NodeGroupID { get; set; } = NodeGroupID.Default;

    [ViewVariables]
    public Entity<NodeGroupComponent>? NodeGroup { get; set; }

    [ViewVariables]
    public EntityUid Owner { get; set; }

    /// <summary>
    ///     Name of this node on the owning <see cref="NodeContainerComponent"/>.
    /// </summary>
    [ViewVariables]
    public string Name { get; set; }

    [DataField]
    public bool NeedAnchored { get; set; } = true;

    [DataField]
    public bool Examinable { get; set; }

    /// <summary>
    ///    Prevents a node from being used by other nodes while midway through removal.
    /// </summary>
    [ViewVariables]
    public bool Deleting { get; set; }

    /// <summary>
    ///     All compatible nodes that are reachable by this node.
    ///     Effectively, active connections out of this node.
    /// </summary>
    [ViewVariables]
    public HashSet<Node> ReachableNodes { get; set; } = new();

    public int FloodGen { get; set; }
    public int UndirectGen { get; set; }
    public bool FlaggedForFlood { get; set; }
    public int NetId { get; set; }
}
