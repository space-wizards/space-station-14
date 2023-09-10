using Content.Shared.Nodes.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Nodes.Components;

/// <summary>
/// </summary>
[Access(typeof(SharedNodeGraphSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GraphNodeComponent : Component
{
    /// <summary>
    /// The type of graph this node can be a part of.
    /// </summary>
    [DataField("graphProto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GraphProto = default!;

    /// <summary>
    /// The id of the node graph this node is a part of.
    /// Should be nonnull at all reachable times between map init and shutdown.
    /// </summary>
    /// <remarks>
    /// Needs custom networking to obfuscate whether pipes disconnected on the client due to PVS are part of the same pipenet on the server.
    /// Not sure how to do that rn so I'm just not going to network it until something occurs to me.
    /// </remarks>
    [AutoNetworkedField] // TODO: Custom networking.
    [ViewVariables]
    public EntityUid? GraphId = null;

    /// <summary>
    /// State flags for this graph node. Usually used to mark transiant states such as being a split/merge location.
    /// </summary>
    [ViewVariables]
    public NodeFlags Flags = NodeFlags.None;

    /// <summary>
    /// The set of graph nodes that this graph node is directly connected to.
    /// </summary>
    /// <remarks>
    /// Defaults to a capacity of 4 because the most common types of nodes are cardinally connected.
    /// </remarks>
    [AutoNetworkedField]
    [DataField("edges")]
    public HashSet<EntityUid> Edges = new(4);

    /// <summary>
    /// The last time this node was processed for the purpose of splitting its group.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables]
    public TimeSpan LastUpdate = default!;

    /// <summary>
    /// The color used to render this node in the debugging overlay.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables]
    public Color? DebugColor = null;
}

/// <summary>
/// </summary>
public enum NodeFlags : byte
{
    /// <summary>
    /// </summary>
    None = 0,
    /// <summary>
    /// Indicates that the node has been initialized.
    /// </summary>
    Init = 1 << 0,
    /// <summary>
    /// Indicates that the node has been queued to have its edges recalculated.
    /// </summary>
    Edges = 1 << 1,
    /// <summary>
    /// Indicates that the node has an edge that may have merged the parent graph with another graph.
    /// </summary>
    Merge = 1 << 2,
    /// <summary>
    /// Indicates that the node has lost an edge that may have split the parent graph.
    /// </summary>
    Split = 1 << 3,
}
