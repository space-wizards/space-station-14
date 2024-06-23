using Content.Shared.Destructible.Thresholds;
using Content.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// This is used for handling interactions with artifacts as well as
/// storing data about artifact node graphs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem)), AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoArtifactComponent : Component
{
    public static string NodeContainerId = "node-container";

    [DataField]
    public bool DoGeneration = true;

    [ViewVariables]
    public Container NodeContainer = default!;

    /// <summary>
    /// The nodes in this artifact that are currently "active."
    /// This is cached and updated when nodes are removed, added, or unlocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<NetEntity> CachedActiveNodes = new();

    [DataField, AutoNetworkedField]
    public List<List<NetEntity>> CachedSegments = new();

    #region Unlocking
    /// <summary>
    /// How long does the unlocking state last.
    /// </summary>
    [DataField]
    public TimeSpan UnlockStateDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum waiting time between unlock states.
    /// </summary>
    [DataField]
    public TimeSpan UnlockStateRefractory = TimeSpan.FromSeconds(10);

    [DataField, AutoPausedField]
    public TimeSpan NextUnlockTime;
    #endregion

    // NOTE: you should not be accessing any of these values directly. Use the methods in SharedXenoArtifactSystem.Graph
    #region Graph
    /// <summary>
    /// List of all of the nodes currently on this artifact.
    /// Indexes are used as a lookup table for <see cref="NodeAdjacencyMatrix"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity?[] NodeVertices = [];

    /// <summary>
    /// Adjacency matrix that stores connections between this artifact's nodes.
    /// A value of "true" denotes an directed edge from node1 to node2, where the location of the vertex is (node1, node2)
    /// A value of "false" denotes no edge.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<List<bool>> NodeAdjacencyMatrix = new();

    public int NodeAdjacencyMatrixRows => NodeAdjacencyMatrix.Count;
    public int NodeAdjacencyMatrixColumns => NodeAdjacencyMatrix.TryGetValue(0, out var value) ? value.Count : 0;
    #endregion

    #region GenerationInfo
    /// <summary>
    /// The total number of nodes that make up this artifact.
    /// </summary>
    [DataField]
    public MinMax NodeCount = new(10, 24);

    /// <summary>
    /// The amount of nodes that go in each segment.
    /// A segment is an interconnected series of nodes.
    /// </summary>
    [DataField]
    public MinMax SegmentSize = new(5, 8);

    /// <summary>
    /// For each "layer" in a segment (set of nodes with equal depth), how many will we generate?
    /// </summary>
    [DataField]
    public MinMax NodesPerSegmentLayer = new(1, 3);

    [DataField]
    public MinMax ReverseScatterPerLayer = new(0, 2);

    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> EffectWeights = "XenoArtifactEffectsDefault";
    #endregion
}
