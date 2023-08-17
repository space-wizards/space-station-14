using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

/// <summary>
///     General criminal record.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralCriminalRecord
{
    /// <summary>
    ///     Status of the person (None, Wanted, Detained).
    /// </summary>
    [ViewVariables]
    public SecurityStatus? Status = SecurityStatus.None;

    /// <summary>
    ///     Reason of the current status.
    /// </summary>
    [ViewVariables]
    public string Reason = string.Empty;
}