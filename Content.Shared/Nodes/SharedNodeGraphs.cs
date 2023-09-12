using Robust.Shared.Serialization;

namespace Content.Shared.Nodes;

/// <summary>
/// Bitflags that represent the (non endpoint) state for a graph edge between two nodes.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum EdgeFlags : byte
{
    /// <summary></summary>
    None = 0,
    /// <summary>Indicates that the edge has been generated/enforced by an autolinker.</summary>
    Auto = 1 << 0,
    /// <summary>Indicates that the edge was manually established through <see cref="NodeGraphSystem.TryAddEdge"/>.</summary>
    Manual = 1 << 1,
    /// <summary>A mask for the bitflags which indicate what has established this edge.</summary>
    SourceMask = Auto | Manual,
    /// <summary>Indicates that the edge is directed away from the host node.</summary>
    Out = 1 << 2,
    /// <summary>Indicates that the edge is directed towards the host node.</summary>
    In = 1 << 3,
    /// <summary>A mask for the bitflags which indicate directionality.</summary>
    DirMask = Out | In,
    /// <summary>Indicates that node graphs cannot merge over this edge.</summary>
    NoMerge = 1 << 4,
}



/// <summary>A state wrapper used to communicate the visual state of graphs to clients.</summary>
[Serializable, NetSerializable]
public sealed partial class GraphVisState : ComponentState
{
    public Color Color { get; init; }
    public int Size { get; init; }

    public GraphVisState(Color color, int size)
    {
        Color = color;
        Size = size;
    }
}

/// <summary>A state wrapper used to communicate the visual state of nodes to clients.</summary>
[Serializable, NetSerializable]
public sealed partial class NodeVisState : ComponentState
{
    public List<EdgeVisState> Edges { get; init; }
    public EntityUid HostId { get; init; }
    public EntityUid? GraphId { get; init; }
    public string GraphProto { get; init; }

    public NodeVisState(List<EdgeVisState> edges, EntityUid hostId, EntityUid? graphId, string graphProto)
    {
        Edges = edges;
        HostId = hostId;
        GraphId = graphId;
        GraphProto = graphProto;
    }
}

/// <summary>A state wrapper used to communicate the visual state of and edge between two nodes to clients.</summary>
[Serializable, NetSerializable]
public readonly record struct EdgeVisState(EntityUid Id, EdgeFlags Flags)
{
    public void Deconstruct(out EntityUid id, out EdgeFlags flags)
    {
        id = Id;
        flags = Flags;
    }

    public override string ToString()
    {
        return $"{Id}: {Flags:F}";
    }
};

/// <summary>The message sent to the server by clients that want to stat visualizing nodes.</summary>
[Serializable, NetSerializable]
public sealed partial class EnableNodeVisMsg : EntityEventArgs
{
    public bool Enabled { get; init; }

    public EnableNodeVisMsg(bool enabled)
    {
        Enabled = enabled;
    }
}
