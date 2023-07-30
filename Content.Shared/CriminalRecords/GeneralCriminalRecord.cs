using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

/// <summary>
///     General criminal record.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed record GeneralCriminalRecord
{
    /// <summary>
    ///     Status of the person (None, Wanted, Detained).
    /// </summary>
    public SecurityStatus Status = SecurityStatus.None;

    /// <summary>
    ///     Reason of the current status.
    /// </summary>
    public string Reason = string.Empty;
}
