namespace Content.Shared.NodeContainer.NodeGroups;

/// <summary>
///     Maintains a collection of <see cref="Node"/>s, and performs operations requiring a list of
///     all connected <see cref="Node"/>s.
/// </summary>
public interface INodeGroup
{
    bool Remaking { get; }

    /// <summary>
    ///     The list of nodes currently in this group.
    /// </summary>
    List<Node> Nodes { get; }
}
