using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CriminalRecords;

[Serializable, NetSerializable]
public enum GeneralCriminalRecordsConsoleKey : byte
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
///     - there is added new filters and so added new states
///         -SelectedKey null, Record null, RecordListing null, filters non-null
///            the station may have data, but they all did not pass through the filters
///
///     Other states are erroneous.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralCriminalRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     Current selected key.
    /// </summary>
    public (NetEntity, uint)? SelectedKey { get; }
    public GeneralCriminalRecord? CriminalRecord { get; }
    public GeneralStationRecord? StationRecord { get; }
    public Dictionary<(NetEntity, uint), string>? RecordListing { get; }
    public GeneralStationRecordsFilter? Filter { get; }

    public GeneralCriminalRecordsConsoleState((NetEntity, uint)? key, GeneralStationRecord? stationRecord, GeneralCriminalRecord? criminalRecord, Dictionary<(NetEntity, uint), string>? recordListing, GeneralStationRecordsFilter? newFilter)
    {
        SelectedKey = key;
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
        RecordListing = recordListing;
        Filter = newFilter;
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && CriminalRecord == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class SelectGeneralCriminalRecord : BoundUserInterfaceMessage
{
    public (NetEntity, uint)? SelectedKey { get; }

    public SelectGeneralCriminalRecord((NetEntity, uint)? selectedKey)
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
public sealed class CriminalRecordWantedButtonPressed : BoundUserInterfaceMessage
{
    public string? Reason;
    public string? Name;

    public CriminalRecordWantedButtonPressed(string? reason, string? name)
    {
        Reason = reason;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordReleaseButtonPressed : BoundUserInterfaceMessage
{
    public string? Reason;
    public string? Name;

    public CriminalRecordReleaseButtonPressed(string? reason, string? name)
    {
        Reason = reason;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordsConsoleDataUIMessage
{
    public readonly GeneralStationRecord StationRecord;
    public readonly GeneralCriminalRecord CriminalRecord;

    public CriminalRecordsConsoleDataUIMessage(GeneralStationRecord stationRecord, GeneralCriminalRecord criminalRecord)
    {
        StationRecord = stationRecord;
        CriminalRecord = criminalRecord;
    }
}