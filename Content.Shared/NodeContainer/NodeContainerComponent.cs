using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.NodeContainer;

/// <summary>
///     Creates and maintains a set of <see cref="Rope.Node"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeContainerComponent : Component
{
    //HACK: THIS BEING readOnly IS A FILTHY HACK AND I HATE IT --moony
    [DataField(readOnly: true, serverOnly: true)] public Dictionary<string, Node> Nodes { get; private set; } = new();

    [DataField] public bool Examinable = false;
}
