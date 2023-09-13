using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

[RegisterComponent, Access(typeof(ArtifactSystem))]
public sealed partial class ArtifactComponent : Component
{
    /// <summary>
    /// Every node contained in the tree
    /// </summary>
    [DataField("nodeTree"), ViewVariables]
    public List<ArtifactNode> NodeTree = new();

    /// <summary>
    /// The current node the artifact is on.
    /// </summary>
    [DataField("currentNodeId"), ViewVariables]
    public int? CurrentNodeId;

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
    [DataField("timer"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Is this artifact under some suppression device?
    /// f true, will ignore all trigger activations attempts.
    /// </summary>
    [DataField("isSuppressed"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsSuppressed;

    /// <summary>
    /// The last time the artifact was activated.
    /// </summary>
    [DataField("lastActivationTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastActivationTime;

    /// <summary>
    /// A multiplier applied to the calculated point value
    /// to determine the monetary value of the artifact
    /// </summary>
    [DataField("priceMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float PriceMultiplier = 0.05f;

    /// <summary>
    /// The base amount of research points for each artifact node.
    /// </summary>
    [DataField("pointsPerNode"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerNode = 5000;

    /// <summary>
    /// Research points which have been "consumed" from the theoretical max value of the artifact.
    /// </summary>
    [DataField("consumedPoints"), ViewVariables(VVAccess.ReadWrite)]
    public int ConsumedPoints;

    /// <summary>
    /// A multiplier that is raised to the power of the average depth of a node.
    /// Used for calculating the research point value of an artifact node.
    /// </summary>
    [DataField("pointDangerMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float PointDangerMultiplier = 1.35f;

    /// <summary>
    /// The sound that plays when an artifact is activated
    /// </summary>
    [DataField("activationSound")]
    public SoundSpecifier ActivationSound = new SoundCollectionSpecifier("ArtifactActivation")
    {
        Params = new()
        {
            Variation = 0.1f,
            Volume = 3f
        }
    };

    [DataField("activateActionEntity")] public EntityUid? ActivateActionEntity;
}

/// <summary>
/// A single "node" of an artifact that contains various data about it.
/// </summary>
[DataDefinition]
public sealed partial class ArtifactNode : ICloneable
{
    /// <summary>
    /// A numeric id corresponding to each node.
    /// </summary>
    [DataField("id"), ViewVariables]
    public int Id;

    /// <summary>
    /// how "deep" into the node tree. used for generation and price/value calculations
    /// </summary>
    [DataField("depth"), ViewVariables]
    public int Depth;

    /// <summary>
    /// A list of surrounding nodes. Used for tree traversal
    /// </summary>
    [DataField("edges"), ViewVariables]
    public HashSet<int> Edges = new();

    /// <summary>
    /// Whether or not the node has been entered
    /// </summary>
    [DataField("discovered"), ViewVariables(VVAccess.ReadWrite)]
    public bool Discovered;

    /// <summary>
    /// The trigger for the node
    /// </summary>
    [DataField("trigger", customTypeSerializer: typeof(PrototypeIdSerializer<ArtifactTriggerPrototype>), required: true), ViewVariables]
    public string Trigger = default!;

    /// <summary>
    /// Whether or not the node has been triggered
    /// </summary>
    [DataField("triggered"), ViewVariables(VVAccess.ReadWrite)]
    public bool Triggered;

    /// <summary>
    /// The effect when the node is activated
    /// </summary>
    [DataField("effect", customTypeSerializer: typeof(PrototypeIdSerializer<ArtifactEffectPrototype>), required: true), ViewVariables]
    public string Effect = default!;

    /// <summary>
    /// Used for storing cumulative information about nodes
    /// </summary>
    [DataField("nodeData"), ViewVariables]
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
