using Content.Shared.Nodes.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Nodes.Components;

/// <summary>
/// </summary>
[Access(typeof(SharedNodeGraphSystem))]
[RegisterComponent]
public sealed partial class NodeGraphComponent : Component
{
    /// <summary>
    /// The default color used to visualize node graphs in the debugging overlay.
    /// </summary>
    [Access(typeof(SharedNodeGraphSystem), Other = AccessPermissions.ReadExecute)]
    [ViewVariables]
    public static readonly Color DefaultColor = Color.Fuchsia;

    /// <summary>
    /// The type of graph this graph can split/merge into.
    /// </summary>
    [ViewVariables]
    public string? GraphProto = default!;

    /// <summary>
    /// The set of all graph nodes in this graph.
    /// </summary>
    /// <remarks>
    /// Not networked because we don't want to spam each client with nodes they don't need to know about.
    /// </remarks>
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
    public TimeSpan? LastUpdate = null;

    /// <summary>
    /// The color used to represent this group in the debugging overlay.
    /// </summary>
    [DataField("color", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color? DebugColor = null;
}
