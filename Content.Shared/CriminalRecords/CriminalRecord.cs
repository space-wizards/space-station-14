using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

/// <summary>
/// Criminal record for a crewmember.
/// Can be viewed and edited in a criminal records console by security.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed record CriminalRecord
{
    /// <summary>
    /// Status of the person (None, Wanted, Detained).
    /// </summary>
    public SecurityStatus Status = SecurityStatus.None;

    /// <summary>
    /// Reason of the current status.
    /// </summary>
    public string Reason = string.Empty;
}
