using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.GameStates;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Represents an entity in a node network that can be crawled in
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
[Access(typeof(SharedNodeCrawlSystem), typeof(NodeCrawlerMovementSystem))]
public sealed partial class CrawlableNodeComponent : Component
{
    /// <summary>
    /// Node type prototypes that can be connected to by this node
    /// </summary>
    [DataField(required: true)]
    public List<NodeGroupID> ReachableNodeTypes = [];

    /// <summary>
    /// Other entities with <see cref="CrawlableNodeComponent" /> that can be reached from this one
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> ReachableNodes = [];

    /// <summary>
    /// Whether this node has an unconnected node and should be exited from on movement.
    /// This is derived runtime state, not prototype data.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool DeadEnd;

    /// <summary>
    /// All entities with <see cref="NodeCrawlerMovementComponent" /> that are associated with this one
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Crawlers = [];
}
