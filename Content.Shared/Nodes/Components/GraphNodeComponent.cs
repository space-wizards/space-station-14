using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nodes.Components;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GraphNodeComponent : Component
{
    /// <summary>
    /// The set of graph nodes that this graph node is directly connected to.
    /// </summary>
    /// <remarks>
    /// Defaults to a capacity of 4 because the most common types of nodes are cardinally connected.
    /// </remarks>
    [AutoNetworkedField]
    [DataField("edges")]
    public HashSet<EntityUid> Edges = new(4);
}
