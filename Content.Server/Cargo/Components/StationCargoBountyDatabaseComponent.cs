using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Stores all active cargo bounties for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCargoBountyDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of bounties a station can have.
    /// </summary>
    [DataField]
    public int MaxBounties = 6;

    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField]
    public List<CargoBountyData> Bounties = new();

    /// <summary>
    /// Maximum amount of people who can claim a bounty.
    /// </summary>
    [DataField]
    public int MaxClaimants = 1;

    /// <summary>
    /// Default status of a new bounty.
    /// </summary>
    [DataField]
    public ProtoId<CargoBountyStatusPrototype> DefaultStatus = "Undelivered";

    /// <summary>
    /// A list of all the bounties that have been completed or
    /// skipped for a station.
    /// </summary>
    [DataField]
    public List<CargoBountyHistoryData> History = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [DataField]
    public int TotalBounties;

    /// <summary>
    /// A list of bounty IDs that have been checked this tick.
    /// Used to prevent multiplying bounty prices.
    /// </summary>
    [DataField]
    public HashSet<string> CheckedBounties = new();

    /// <summary>
    /// The group that bounties are pulled from.
    /// </summary>
    [DataField]
    public ProtoId<CargoBountyGroupPrototype> Group = "StationBounty";

    /// <summary>
    /// The time at which players will be able to skip the next bounty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The time at which players will be able to register a claimant on a bounty again.
    /// </summary>
    [DataField("nextClaimTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextClaimTime = TimeSpan.Zero;

    /// <summary>
    /// The time between claims.
    /// </summary>
    [DataField("claimDelay")]
    public TimeSpan ClaimDelay = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// The time at which players will be able to update the status of a bounty again..
    /// </summary>
    [DataField("nextStatusUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextStatusUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// The time between status updates.
    /// </summary>
    [DataField("statusUpdateDelay")]
    public TimeSpan StatusUpdateDelay = TimeSpan.FromSeconds(1);
}
