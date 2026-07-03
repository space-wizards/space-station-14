using Robust.Shared.GameStates;

namespace Content.Shared.NodeContainer.Components;

/// <summary>
///     Creates and maintains a set of <see cref="Node"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeContainerComponent : Component
{
    [DataField]
    public Dictionary<string, Node> Nodes = new();
}
