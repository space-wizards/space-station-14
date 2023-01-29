using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed class ArtifactComponent : Component
{
    /// <summary>
    /// The artifact's node tree.
    /// </summary>
    [ViewVariables]
    public ArtifactTree? NodeTree;

    /// <summary>
    /// The current node the artifact is on.
    /// </summary>
    [ViewVariables]
    public ArtifactNode? CurrentNode;

    #region Node Tree Gen
    /// <summary>
    /// Minimum number of nodes to generate, inclusive
    /// </summary>
    [DataField("nodesMin")]
    public int NodesMin = 3;

    /// <summary>
    /// Maximum number of nodes to generate, exclusive
    /// </summary>
    [DataField("nodesMax")]
    public int NodesMax = 9;
    #endregion

    /// <summary>
    /// Cooldown time between artifact activations (in seconds).
    /// </summary>
    [DataField("timer", customTypeSerializer: typeof(TimespanSerializer))]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Is this artifact under some suppression device?
    /// f true, will ignore all trigger activations attempts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsSuppressed;

    /// <summary>
    /// The last time the artifact was activated.
    /// </summary>
    [DataField("lastActivationTime", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan LastActivationTime;

    /// <summary>
    /// The base price of each node for an artifact
    /// </summary>
    [DataField("pricePerNode")]
    public int PricePerNode = 500;

    /// <summary>
    /// The base amount of research points for each artifact node.
    /// </summary>
    [DataField("pointsPerNode")]
    public int PointsPerNode = 5000;

    /// <summary>
    /// A multiplier that is raised to the power of the average depth of a node.
    /// Used for calculating the research point value of an artifact node.
    /// </summary>
    [DataField("pointDangerMultiplier")]
    public float PointDangerMultiplier = 1.35f;
}

/// <summary>
/// A tree of nodes.
/// </summary>
[DataDefinition]
public sealed class ArtifactTree
{
    /// <summary>
    /// The first node of the tree
    /// </summary>
    [ViewVariables]
    public ArtifactNode StartNode = default!;

    /// <summary>
    /// Every node contained in the tree
    /// </summary>
    [ViewVariables]
    public readonly List<ArtifactNode> AllNodes = new();
}

/// <summary>
/// A single "node" of an artifact that contains various data about it.
/// </summary>
[DataDefinition]
public sealed class ArtifactNode : ICloneable
{
    /// <summary>
    /// A numeric id corresponding to each node. used for display purposes
    /// </summary>
    [ViewVariables]
    public int Id;

    /// <summary>
    /// how "deep" into the node tree. used for generation and price/value calculations
    /// </summary>
    [ViewVariables]
    public int Depth = 0;

    /// <summary>
    /// A list of surrounding nodes. Used for tree traversal
    /// </summary>
    [ViewVariables]
    public List<ArtifactNode> Edges = new();

    /// <summary>
    /// Whether or not the node has been entered
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Discovered = false;

    /// <summary>
    /// The trigger for the node
    /// </summary>
    [ViewVariables]
    public ArtifactTriggerPrototype Trigger = default!;

    /// <summary>
    /// Whether or not the node has been triggered
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Triggered = false;

    /// <summary>
    /// The effect when the node is activated
    /// </summary>
    [ViewVariables]
    public ArtifactEffectPrototype Effect = default!;

    /// <summary>
    /// Used for storing cumulative information about nodes
    /// </summary>
    [ViewVariables]
    public Dictionary<string, object> NodeData = new();

    public object Clone()
    {
        return new ArtifactNode
        {
            Id = Id,
            Depth = Depth,
            Edges = Edges,
            Discovered = Discovered,
            Trigger = Trigger,
            Triggered = Triggered,
            Effect = Effect,
            NodeData = NodeData
        };
    }
}
