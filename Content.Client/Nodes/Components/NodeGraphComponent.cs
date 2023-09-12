using Robust.Shared.GameStates;

namespace Content.Client.Nodes.Components;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeGraphComponent : Component
{
    /// <summary>
    /// </summary>
    [ViewVariables]
    public static readonly Color DefaultColor = Color.Fuchsia;

    /// <summary>
    /// The color used to render this graph in the debugging overlay.
    /// </summary>
    [ViewVariables]
    public Color VisColor = DefaultColor;

    /// <summary>
    /// The number of nodes that exist in this graph.
    /// </summary>
    [ViewVariables]
    public int Size = 0;
}
