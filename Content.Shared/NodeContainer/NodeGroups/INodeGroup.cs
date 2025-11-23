using System.Linq;

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
    IReadOnlyList<Node> Nodes { get; }

    void Create(NodeGroupID groupId);

    void Initialize(Node sourceNode, IEntityManager entMan);

    void RemoveNode(Node node);

    void LoadNodes(List<Node> groupNodes);

    // In theory, the SS13 curse ensures this method will never be called.
    void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups);

    /// <summary>
    ///     Return any additional data to display for the node-visualizer debug overlay.
    /// </summary>
    string? GetDebugData();
}