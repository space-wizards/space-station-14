using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

[Serializable, NetSerializable]
public enum CriminalRecordsConsoleKey : byte
{
    Key
}

/// <summary>
///     Criminal records console state. There are a few states:
///     - SelectedKey null, Record null, RecordListing null
///         - The station record database could not be accessed.
///     - SelectedKey null, Record null, RecordListing non-null
///         - Records are populated in the database, or at least the station has
///           the correct component.
///     - SelectedKey non-null, Record null, RecordListing non-null
///         - The selected key does not have a record tied to it.
///     - SelectedKey non-null, Record non-null, RecordListing non-null
///         - The selected key has a record tied to it, and the record has been sent.
///
///     - there is added new filters and so added new states
///         -SelectedKey null, Record null, RecordListing null, filters non-null
///            the station may have data, but they all did not pass through the filters
///
///     Other states are erroneous.
/// </summary>
[Serializable, NetSerializable]
public sealed class CriminalRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Currently selected crewmember record key.
    /// </summary>
    public readonly uint? SelectedKey;

    public readonly CriminalRecord? CriminalRecord;
    public readonly GeneralStationRecord? StationRecord;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;

    public CriminalRecordsConsoleState(uint? key, GeneralStationRecord? stationRecord, CriminalRecord? criminalRecord, Dictionary<uint, string>? recordListing, StationRecordsFilter? newFilter)
    {
        SelectedKey = key;
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
        RecordListing = recordListing;
        Filter = newFilter;
    }

    /// <summary>
    /// Default state for opening the console
    /// </summary>
    public CriminalRecordsConsoleState() : this(null, null, null, null, null)
    {
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && CriminalRecord == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class CriminalRecordArrestButtonPressed : BoundUserInterfaceMessage
{
    public readonly string Reason;
    public readonly string Name;

    public CriminalRecordArrestButtonPressed(string reason, string name)
    {
        Reason = reason;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalStatusOptionButtonSelected : BoundUserInterfaceMessage
{
    public readonly SecurityStatus Status;
    public readonly string Reason;
    public readonly string Name;

    public CriminalStatusOptionButtonSelected(SecurityStatus status, string reason, string name)
    {
        Status = status;
        Reason = reason;
        Name = name;
    }
}

/// <summary>
/// Used to add a single line to the record's crime history.
/// </summary>
[Serializable, NetSerializable]
public sealed class CriminalRecordAddHistory : BoundUserInterfaceMessage
{
    public readonly string Line;

    public CriminalRecordAddHistory(string line)
    {
        Line = line;
    }
}

/// <summary>
/// Used to delete a single line from the crime history, by index.
/// </summary>
[Serializable, NetSerializable]
public sealed class CriminalRecordDeleteHistory : BoundUserInterfaceMessage
{
    public readonly uint Index;

    public CriminalRecordDeleteHistory(uint index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordConsoleDataUIMessage
{
    public readonly GeneralStationRecord StationRecord;
    public readonly CriminalRecord CriminalRecord;

    public CriminalRecordConsoleDataUIMessage(GeneralStationRecord stationRecord, CriminalRecord criminalRecord)
    {
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
    }
}
