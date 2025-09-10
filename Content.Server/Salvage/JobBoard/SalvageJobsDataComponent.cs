using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.JobBoard;

/// <summary>
/// holds information for a station relating to the salvage job board
/// </summary>
[RegisterComponent]
[Access(typeof(SalvageJobBoardSystem))]
public sealed partial class SalvageJobsDataComponent : Component
{
    /// <summary>
    /// A dictionary relating the number of completed jobs needed to the different ranks.
    /// </summary>
    [DataField]
    public SortedDictionary<int, SalvageRankDatum> RankThresholds = new();

    /// <summary>
    /// The rank given when all salvage jobs are complete.
    /// </summary>
    [DataField]
    public SalvageRankDatum MaxRank;

    /// <summary>
    /// A list of all completed jobs in order.
    /// </summary>
    [DataField]
    public List<ProtoId<CargoBountyPrototype>> CompletedJobs = new();

    /// <summary>
    /// Account where rewards are deposited.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> RewardAccount = "Cargo";
}

/// <summary>
/// Holds information about salvage job ranks
/// </summary>
[DataDefinition]
public partial record struct SalvageRankDatum
{
    /// <summary>
    /// The title displayed when this rank is reached
    /// </summary>
    [DataField]
    public LocId Title;

    /// <summary>
    /// The bounties associated with this rank.
    /// </summary>
    [DataField]
    public ProtoId<CargoBountyGroupPrototype>? BountyGroup;

    /// <summary>
    /// The market that is unlocked when you reach this rank
    /// </summary>
    [DataField]
    public ProtoId<CargoMarketPrototype>? UnlockedMarket;
}
