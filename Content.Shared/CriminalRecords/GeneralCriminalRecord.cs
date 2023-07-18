using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

/// <summary>
///     General criminal record.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public record struct GeneralCriminalRecord(
    SecurityStatus Status = default,
    string Reason = ""
);
