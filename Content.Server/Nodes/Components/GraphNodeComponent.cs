using Content.Server.Nodes.EntitySystems;
using Content.Shared.Nodes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Nodes.Components;

/// <summary>
/// </summary>
[Access(typeof(NodeGraphSystem))]
[RegisterComponent]
public sealed partial class GraphNodeComponent : Component
{
    /// <inheritdoc />
    /// <remarks>Node graphs are mostly server only, but there's a debugging overlay that shows them. As a result, which clients get the node state depends on who has debugging access.</remarks>
    public override bool SessionSpecific => true;


    /// <summary>
    /// The type of graph this node can be a part of.
    /// </summary>
    [DataField("graphProto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GraphProto = default!;

    /// <summary>
    /// The id of the node graph this node is a part of.
    /// Should only be null before mapinit and after component shutdown.
    /// </summary>
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
    [DataField("edges")]
    public List<Edge> Edges = new(4);

    /// <summary>
    /// The number of edges this node has that can be merged over.
    /// </summary>
    [ViewVariables]
    public int NumMergeableEdges = 0;

    /// <summary>
    /// The last time this node was processed for the purpose of splitting its group.
    /// </summary>
    [ViewVariables]
    public UpdateIter? LastUpdate = null;
}

/// <summary>
/// </summary>
public enum NodeFlags : byte
{
    /// <summary></summary>
    None = 0,
    /// <summary>Indicates that the node has been initialized.</summary>
    Init = 1 << 0,
    /// <summary>Indicates that the node has been queued to have its edges recalculated.</summary>
    Edges = 1 << 1,
    /// <summary>Indicates that the node has an edge that may have merged the parent graph with another graph.</summary>
    Merge = 1 << 2,
    /// <summary>Indicates that the node has lost an edge that may have split the parent graph.</summary>
    Split = 1 << 3,
}

/// <summary>
/// </summary>
[DataDefinition]
public readonly partial struct Edge
{
    /// <summary>
    /// The flags that effectively exist for edges that do not exist.
    /// </summary>
    [ViewVariables]
    public const EdgeFlags NullFlags = EdgeFlags.NoMerge;

    /// <summary>
    /// The default state of an edge.
    /// </summary>
    [ViewVariables]
    public const EdgeFlags DefaultFlags = EdgeFlags.None;


    /// <summary>
    /// The entity at the other end of this edge.
    /// </summary>
    [DataField("id", required: true)]
    public EntityUid Id { get; init; }

    /// <summary>
    /// The current state of this edge as bitflags.
    /// </summary>
    [DataField("flags")]
    public EdgeFlags Flags { get; init; }


    /// <summary>
    /// Creates a new edge.
    /// </summary>
    public Edge(EntityUid id, EdgeFlags flags = DefaultFlags)
    {
        Id = id;
        Flags = flags;
    }

    /// <summary>
    /// Extracts the internal state of this edge.
    /// </summary>
    public void Deconstruct(out EntityUid id, out EdgeFlags flags)
    {
        id = Id;
        flags = Flags;
    }

    public override string ToString()
    {
        return $"{Id}: {(byte) Flags:b8}";
    }
}

/// <summary>
/// Host for static <see cref="Edge"/> and <see cref="EdgeFlags"/> extension methods.
/// </summary>
public static class EdgeHelpers
{
    /// <summary>
    /// Returns a copy of some edge flags with the direction inverted.
    /// </summary>
    public static EdgeFlags Invert(this EdgeFlags flags)
    {
        switch (flags & EdgeFlags.DirMask)
        {
            case EdgeFlags.Out:
                flags = (flags & ~EdgeFlags.DirMask) | EdgeFlags.In;
                break;
            case EdgeFlags.In:
                flags = (flags & ~EdgeFlags.DirMask) | EdgeFlags.Out;
                break;
        }

        return flags;
    }
}
