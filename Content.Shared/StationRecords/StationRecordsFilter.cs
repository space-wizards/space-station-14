using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public sealed class StationRecordsFilter
{
    public StationRecordFilterType Type = StationRecordFilterType.Name;
    public string Value  = "";

    public StationRecordsFilter(StationRecordFilterType filterType, string newValue = "")
    {
        Type = filterType;
        Value = newValue;
    }
}

/// <summary>
/// Message for updating the filter on any kind of records console.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetStationRecordFilter : BoundUserInterfaceMessage
{
    public readonly string Value;
    public readonly StationRecordFilterType Type;

    public SetStationRecordFilter(StationRecordFilterType filterType,
        string filterValue)
    {
        Type = filterType;
        Value = filterValue;
    }
}

/// <summary>
/// Different strings that results can be filtered by.
/// </summary>
[Serializable, NetSerializable]
public enum StationRecordFilterType : byte
{
    Name,
    Job,
    Species,
    Prints,
    DNA,
}
