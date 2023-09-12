using Content.Server.Nodes.EntitySystems;

namespace Content.Server.Nodes.Components;

/// <summary>
/// </summary>
[Access(typeof(NodeGraphSystem))]
[RegisterComponent]
public sealed partial class NodeGraphComponent : Component
{
    /// <inheritdoc />
    /// <remarks>Node graphs are mostly server only, but there's a debugging overlay that shows them. As a result, which clients get the graph state depends on who has debugging access.</remarks>
    public override bool SessionSpecific => true;

    /// <summary>
    /// The color used to represent this graph in the debugging overlay. Alpha is ignored.
    /// </summary>
    [DataField("color", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color DebugColor = default!;


    /// <summary>
    /// The type of graph this graph can split/merge into. Overridden with the entity prototype of the graph.
    /// </summary>
    [ViewVariables]
    public string GraphProto = default!;

    /// <summary>
    /// The set of all graph nodes in this graph.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Nodes = new();

    /// <summary>
    /// The set of all nodes in this graph that may have been linked to nodes in a different graph (that this graph can merge with).
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> MergeNodes = new();

    /// <summary>
    /// The set of all nodes in this graph that have been unlinked from nodes in this graph. These may be points where the graph has split into two or more smaller graphs.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> SplitNodes = new();

    /// <summary>
    /// The last time this graph was processed merging with other graphs.
    /// </summary>
    [ViewVariables]
    public UpdateIter? LastUpdate = null;
}
