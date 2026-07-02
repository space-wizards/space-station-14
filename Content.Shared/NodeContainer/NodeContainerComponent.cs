using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.NodeContainer;

/// <summary>
///     Creates and maintains a set of <see cref="Rope.Node"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeContainerComponent : Component
{
    [DataField]
    public Dictionary<string, Node> Nodes = new();
}
