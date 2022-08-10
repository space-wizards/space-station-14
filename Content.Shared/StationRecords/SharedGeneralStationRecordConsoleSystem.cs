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
///     Other states are erroneous.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralStationRecordConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     Current selected key.
    /// </summary>
    public StationRecordKey? SelectedKey { get; }
    public GeneralStationRecord? Record { get; }
    public Dictionary<StationRecordKey, string>? RecordListing { get; }

    public GeneralStationRecordConsoleState(StationRecordKey? key, GeneralStationRecord? record, Dictionary<StationRecordKey, string>? recordListing)
    {
        SelectedKey = key;
        Record = record;
        RecordListing = recordListing;
    }

    public bool IsEmpty() => SelectedKey == null && Record == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class SelectGeneralStationRecord : BoundUserInterfaceMessage
{
    public StationRecordKey? SelectedKey { get; }

    public SelectGeneralStationRecord(StationRecordKey? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}
