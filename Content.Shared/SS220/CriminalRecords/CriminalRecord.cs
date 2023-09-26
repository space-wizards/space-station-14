// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CriminalRecords;

/// <summary>
///     General station record. Indicates the crewmember's name and job.
/// </summary>
[Serializable, NetSerializable]
public sealed class CriminalRecordCatalog
{
    [DataField]
    public Dictionary<int, CriminalRecord> Records = new();

    [DataField]
    public int? LastRecordTime;

    public CriminalRecord? GetLastRecord()
    {
        if (!LastRecordTime.HasValue)
            return null;

        if (Records.TryGetValue(LastRecordTime.Value, out var record))
            return record;

        return null;
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecord
{
    [DataField]
    public string Message = "";

    [DataField]
    public ProtoId<CriminalStatusPrototype>? RecordType;
}
