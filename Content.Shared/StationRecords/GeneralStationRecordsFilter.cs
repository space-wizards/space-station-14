using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilter {
    public GeneralStationRecordFilterType type { get; set; } =
        GeneralStationRecordFilterType.Name;
    public string value { get; set; } = "";
    public GeneralStationRecordsFilter(GeneralStationRecordFilterType filterType,
        string newValue = "") {
            type = filterType;
            value = newValue;
    }
}

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilterMsg : BoundUserInterfaceMessage
{
    public string value { get; }
    public GeneralStationRecordFilterType type { get; }

    public GeneralStationRecordsFilterMsg(GeneralStationRecordFilterType filterType,
        string  filterValue)
    {
        type = filterType;
        value = filterValue;
    }
}

[Serializable, NetSerializable]
public enum GeneralStationRecordFilterType : byte
{
    Name,
    Prints,
    DNA,
}
