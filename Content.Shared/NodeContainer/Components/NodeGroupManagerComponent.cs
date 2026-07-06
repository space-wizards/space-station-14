using Robust.Shared.GameStates;

namespace Content.Shared.NodeContainer.Components;

/// <summary>
/// A singleton that controls <see cref="NodeGroupComponent"/> entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeGroupManagerComponent : Component
{
    /// <summary>
    /// A set of node groups that have to be remade from scratch.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<Entity<NodeGroupComponent>> ToRemake = new();

    /// <summary>
    /// Current generation of the created node groups.
    /// </summary>
    [ViewVariables]
    public int Generation;

    /// <summary>
    /// A set of nodes that have to be removed from their groups.
    /// </summary>
    [DataField]
    public HashSet<Node> ToRemove = new();

    [DataField]
    public List<Node> ToReflood = new();
}
