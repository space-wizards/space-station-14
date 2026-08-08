// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.PersonnelRecords;

[Serializable, NetSerializable]
public enum PersonnelRecordsConsoleKey : byte
{
    Key
}

/// <summary>
/// Personnel Records console state.
///
/// Selecting/filtering messages reuse <c>SelectStationRecord</c> and <c>SetStationRecordFilter</c>
/// from <see cref="Content.Shared.StationRecords"/> - the same messages the General and Criminal
/// records consoles already use, see <c>Content.Shared/StationRecords/GeneralRecordsUi.cs</c>.
///
/// Button availability flags (<see cref="CanReprimand"/> etc.) are computed server-side from the
/// acting player's ID card and the selected record, on every state rebuild - the client only uses
/// them to grey out buttons, the server independently re-validates every action on receipt.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Currently selected crewmember record key.
    /// </summary>
    public uint? SelectedKey;

    public PersonnelRecord? PersonnelRecord;
    public GeneralStationRecord? StationRecord;

    /// <summary>
    /// Read-only criminal status of the selected crewmember, shown for context. Never written to
    /// from this console except via the explicit "declare wanted" action.
    /// </summary>
    public CriminalRecord? CriminalRecord;

    public EmploymentStatus FilterStatus;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;

    /// <summary>
    /// True if the acting player's card gives them full-crew visibility (Captain/HoP), as opposed
    /// to being scoped to a single department.
    /// </summary>
    public bool FullAccess;

    /// <summary>
    /// True if no department could be determined for the acting player's card and they don't have
    /// full access either - the console should show "department not determined" and an empty list.
    /// </summary>
    public bool NoDepartment;

    public bool CanReprimand;
    public bool CanDemote;
    public bool CanDismiss;
    public bool CanAnnul;
    public bool CanPrint;
    public bool CanDeclareWanted;

    public PersonnelRecordsConsoleState(Dictionary<uint, string>? recordListing, StationRecordsFilter? filter)
    {
        RecordListing = recordListing;
        Filter = filter;
    }

    /// <summary>
    /// Default state for opening the console.
    /// </summary>
    public PersonnelRecordsConsoleState() : this(null, null)
    {
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && PersonnelRecord == null && RecordListing == null;
}

/// <summary>
/// Issues a Reprimand, Demotion or Dismissal order against the selected record.
/// <see cref="Status"/> must be one of those three - None is not a valid order to "issue".
/// Reason is mandatory and re-validated on the server (non-empty, within MaxStringLength).
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordIssueOrder : BoundUserInterfaceMessage
{
    public readonly EmploymentStatus Status;
    public readonly string Reason;

    public PersonnelRecordIssueOrder(EmploymentStatus status, string reason)
    {
        Status = status;
        Reason = reason;
    }
}

/// <summary>
/// Cancels the selected record's active Demotion/Dismissal order, provided it hasn't been executed
/// yet. Reason is mandatory, same validation as <see cref="PersonnelRecordIssueOrder"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordAnnulOrder : BoundUserInterfaceMessage
{
    public readonly string Reason;

    public PersonnelRecordAnnulOrder(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// Sets the employment-status filter for the crew listing (mirrors
/// <c>CriminalRecordSetStatusFilter</c>).
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordSetStatusFilter : BoundUserInterfaceMessage
{
    public readonly EmploymentStatus FilterStatus;

    public PersonnelRecordSetStatusFilter(EmploymentStatus filterStatus)
    {
        FilterStatus = filterStatus;
    }
}

/// <summary>
/// Declares the selected crewmember wanted, delegating to
/// <c>CriminalRecordsSystem.TryChangeStatus(key, SecurityStatus.Wanted, reason)</c>. Only available
/// with full (Captain/HoP) access. No hidden automatic escalation exists anywhere else in this
/// feature - this explicit button is the only path from Personnel Records into Criminal Records.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordDeclareWanted : BoundUserInterfaceMessage
{
    public readonly string Reason;

    public PersonnelRecordDeclareWanted(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// Prints the selected record's currently active order on a command paperwork blank.
/// Only valid while <see cref="PersonnelRecord.Status"/> is not None; rate-limited server-side.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelRecordPrintOrder : BoundUserInterfaceMessage
{
}
