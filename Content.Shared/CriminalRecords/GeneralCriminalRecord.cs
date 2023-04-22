using Content.Shared.Security;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

/// <summary>
///     General station record. Indicates the crewmember's name and job.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralCriminalRecord
{

    /// <summary>
    ///     Status of the person (None, Wanted, Detained).
    /// </summary>
    [ViewVariables]
    public SecurityStatus? Status = SecurityStatus.None;
}
