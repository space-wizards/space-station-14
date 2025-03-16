using Robust.Shared.Prototypes;

namespace Content.Shared.TreeBranch;

[RegisterComponent]
public sealed partial class TreeBranchesComponent : Component
{
    /// <summary>
    /// The current number of branches on the tree.
    /// </summary>
    [DataField("currentBranches")]
    public int CurrentBranches = 0;

    /// <summary>
    /// The maximum number of branches the tree can have.
    /// </summary>
    [DataField("maxBranches")]
    public int MaxBranches = 3;

    /// <summary>
    /// Time (in seconds) between each branch growth.
    /// </summary>
    [DataField("growthTime")]
    public float GrowthTime = 3600.0f; // 1 hour per branch

    /// <summary>
    /// Probability of spawning an item when collecting a branch.
    /// There is a chance that the collection attempt yields no usable branch.
    /// </summary>
    [DataField("spawnProbability")]
    public float SpawnProbability = 0.8f;

    /// <summary>
    /// The timestamp of the last branch growth.
    /// </summary>
    [DataField("lastGrowthTime")]
    public TimeSpan LastGrowthTime = TimeSpan.Zero;

    /// <summary>
    /// The time taken to collect branch from a tree
    /// </summary>
    [DataField("collectionTime")]
    public float CollectionTime { get; set; } = 5.0f;

}