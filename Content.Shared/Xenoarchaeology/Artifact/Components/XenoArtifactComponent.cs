using Content.Shared.Destructible.Thresholds;
using Content.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// This is used for handling interactions with artifacts as well as
/// storing data about artifact node graphs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem))]
public sealed partial class XenoArtifactComponent : Component
{
    public static string NodeContainerId = "node-container";

    [ViewVariables]
    public Container NodeContainer = default!;

    /// <summary>
    /// The nodes in this artifact that are currently "active."
    /// This is cached and updated when nodes are removed, added, or unlocked.
    /// </summary>
    [DataField]
    public List<EntityUid> CachedActiveNodes = new();

    //TODO: can't serialize entityuid arrays. Well fuck.

    // NOTE: you should not be accessing any of these values directly. Use the methods in SharedXenoArtifactSystem.Graph
    #region Graph
    /// <summary>
    /// List of all of the nodes currently on this artifact.
    /// Indexes are used as a lookup table for <see cref="NodeAdjacencyMatrix"/>.
    /// </summary>
    [DataField]
    public EntityUid?[] NodeVertices = [];

    /// <summary>
    /// Adjacency matrix that stores connections between this artifact's nodes.
    /// A value of "true" denotes an directed edge from node1 to node2, where the location of the vertex is (node1, node2)
    /// A value of "false" denotes no edge.
    /// </summary>
    [DataField]
    public bool[,] NodeAdjacencyMatrix = { };

    public int NodeAdjacencyMatrixRows => NodeAdjacencyMatrix.GetLength(0);
    public int NodeAdjacencyMatrixColumns => NodeAdjacencyMatrix.GetLength(1);
    #endregion

    #region GenerationInfo
    /// <summary>
    /// The total number of nodes that make up this artifact.
    /// </summary>
    [DataField]
    public MinMax NodeCount = new(10, 20);

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
    public ProtoId<WeightedRandomEntityPrototype> EffectWeights = "XenoArtifactEffectsDefault";
    #endregion
}

// TODO: manually implement component state. yeesh.
