using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

[Serializable, NetSerializable]
public enum GeneralCriminalRecordConsoleKey : byte
{
    Key
}

/// <summary>
///     General criminal records console state. There are a few states:
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
///     Other states are erroneous.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralCriminalRecordConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     Current selected key.
    /// </summary>
    public StationRecordKey? SelectedKey { get; }
    public GeneralCriminalRecord? CriminalRecord { get; }
    public GeneralStationRecord? StationRecord { get; }
    public Dictionary<StationRecordKey, string>? RecordListing { get; }

    public GeneralCriminalRecordConsoleState(StationRecordKey? key, GeneralStationRecord? stationRecord, GeneralCriminalRecord? criminalRecord, Dictionary<StationRecordKey, string>? recordListing)
    {
        SelectedKey = key;
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
        RecordListing = recordListing;
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && CriminalRecord == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class SelectGeneralCriminalRecord : BoundUserInterfaceMessage
{
    public StationRecordKey? SelectedKey { get; }

    public SelectGeneralCriminalRecord(StationRecordKey? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordArrestButtonPressed : BoundUserInterfaceMessage
{
    public string? Reason;
    public string? Name;

    public CriminalRecordArrestButtonPressed(string? reason, string? name)
    {
        Reason = reason;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalStatusOptionButtonSelected : BoundUserInterfaceMessage
{
    public SecurityStatus Status;
    public string? Reason;
    public string? Name;

    public CriminalStatusOptionButtonSelected(SecurityStatus status, string? reason, string? name)
    {
        Status = status;
        Reason = reason;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordConsoleDataUIMessage
{
    public readonly GeneralStationRecord StationRecord;
    public readonly GeneralCriminalRecord CriminalRecord;

    public CriminalRecordConsoleDataUIMessage(GeneralStationRecord stationRecord, GeneralCriminalRecord criminalRecord)
    {
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
    }
}
