using Content.Shared.Cargo;

namespace Content.Server.Cargo.Components;

/// <summary>
/// Stores all active cargo bounties for a particular station.
/// </summary>
[RegisterComponent]
public sealed class StationCargoBountyDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of bounties a station can have.
    /// </summary>
    [DataField("maxBounties"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxBounties = 3;

    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField("bounties"), ViewVariables(VVAccess.ReadWrite)]
    public List<CargoBountyData> Bounties = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [DataField("totalBounties")]
    public int TotalBounties;

    /// <summary>
    /// A poor-man's weighted list of the durations for how long
    /// each bounty will last.
    /// </summary>
    [DataField("bountyDurations")]
    public List<TimeSpan> BountyDurations = new()
    {
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(7.5f),
        TimeSpan.FromMinutes(7.5f),
        TimeSpan.FromMinutes(7.5f),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15)
    };
}
