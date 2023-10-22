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
    ///     Current selected key.
    /// </summary>
    public (NetEntity, uint)? SelectedKey { get; }
    public GeneralStationRecord? Record { get; }
    public Dictionary<(NetEntity, uint), string>? RecordListing { get; }
    public GeneralStationRecordsFilter? Filter { get; }
    public GeneralStationRecordConsoleState((NetEntity, uint)? key, GeneralStationRecord? record,
        Dictionary<(NetEntity, uint), string>? recordListing, GeneralStationRecordsFilter? newFilter)
    {
        SelectedKey = key;
        Record = record;
        RecordListing = recordListing;
        Filter = newFilter;
    }

    public bool IsEmpty() => SelectedKey == null
        && Record == null && RecordListing == null;
}

[Serializable, NetSerializable]
public sealed class SelectGeneralStationRecord : BoundUserInterfaceMessage
{
    public (NetEntity, uint)? SelectedKey { get; }

    public SelectGeneralStationRecord((NetEntity, uint)? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}
