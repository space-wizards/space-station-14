using Content.Shared.Cargo;
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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxBounties = 6;

    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<CargoBountyData> Bounties = new();

    /// <summary>
    /// A list of all the bounties that have been completed or
    /// skipped for a station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
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
    /// The time at which players will be able to skip the next bounty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(15);
}
