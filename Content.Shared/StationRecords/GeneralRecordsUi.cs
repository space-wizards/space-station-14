using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public enum GeneralStationRecordConsoleKey : byte
{
    Key
}

/// <summary>
///     General station records console state. There are a few states:
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
public sealed class GeneralStationRecordConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Current selected key.
    /// Station is always the station that owns the console.
    /// </summary>
    public readonly uint? SelectedKey;
    public readonly GeneralStationRecord? Record;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;

    public GeneralStationRecordConsoleState(uint? key, GeneralStationRecord? record,
        Dictionary<uint, string>? recordListing, StationRecordsFilter? newFilter)
    {
        SelectedKey = key;
        Record = record;
        RecordListing = recordListing;
        Filter = newFilter;
    }

    public GeneralStationRecordConsoleState() : this(null, null, null, null)
    {
    }

    public bool IsEmpty() => SelectedKey == null
        && Record == null && RecordListing == null;
}

/// <summary>
/// Select a specific crewmember's record, or deselect.
/// Used by any kind of records console including general and criminal.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectStationRecord : BoundUserInterfaceMessage
{
    public readonly uint? SelectedKey;

    public SelectStationRecord(uint? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}
