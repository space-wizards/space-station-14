using Content.Shared.Cargo;

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
    /// The minimum amount of time the bounty lasts before being removed.
    /// </summary>
    [DataField("minBountyTime"), ViewVariables(VVAccess.ReadWrite)]
    public float MinBountyTime = 600f;

    /// <summary>
    /// The maximum amount of time the bounty lasts before being removed.
    /// </summary>
    [DataField("maxBountyTime"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxBountyTime = 905f;

    /// <summary>
    /// A list of bounty IDs that have been checked this tick.
    /// Used to prevent multiplying bounty prices.
    /// </summary>
    [DataField]
    public HashSet<int> CheckedBounties = new();
}
