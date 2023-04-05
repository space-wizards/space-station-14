using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilter
{
    public GeneralStationRecordFilterType type { get; set; }
        = GeneralStationRecordFilterType.Name;
    public string value { get; set; } = "";
    public GeneralStationRecordsFilter(GeneralStationRecordFilterType filterType, string newValue = "")
    {
        type = filterType;
        value = newValue;
    }

    public bool IsSkippedRecord(GeneralStationRecord someRecord)
    {
        bool isSkipRecord = false;
        bool isFilter = value.Length > 0;
        string filterLowerCaseValue = "";

        if (isFilter)
        {
            filterLowerCaseValue = value.ToLower();
        }

        switch (type)
        {
            case GeneralStationRecordFilterType.Name:
                if (someRecord.Name != null)
                {
                    string loweName = someRecord.Name.ToLower();
                    isSkipRecord = isFilter && !loweName.Contains(filterLowerCaseValue);
                }
                break;
            case GeneralStationRecordFilterType.Prints:
                if (someRecord.Fingerprint != null)
                {
                    isSkipRecord = isFilter
                        && IsFilterWithCodeValue(someRecord.Fingerprint, filterLowerCaseValue);
                }
                break;
            case GeneralStationRecordFilterType.DNA:
                if (someRecord.DNA != null)
                {
                    isSkipRecord = isFilter
                        && IsFilterWithCodeValue(someRecord.DNA, filterLowerCaseValue);
                }
                break;
        }

        return isSkipRecord;
    }

    private static bool IsFilterWithCodeValue(string value, string filter)
    {
        string lowerValue = value.ToLower();
        return !lowerValue.StartsWith(filter);
    }
}

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilterMsg : BoundUserInterfaceMessage
{
    public string value { get; }
    public GeneralStationRecordFilterType type { get; }

    public GeneralStationRecordsFilterMsg(GeneralStationRecordFilterType filterType,
        string filterValue)
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
