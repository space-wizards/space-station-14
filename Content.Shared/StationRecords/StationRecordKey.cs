using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

// Station record keys. These should be stored somewhere,
// preferably within an ID card.
[Serializable, NetSerializable]
public readonly struct StationRecordKey
{
    [ViewVariables]
    public uint ID { get; }

    [ViewVariables]
    public EntityUid OriginStation { get; }

    public StationRecordKey(uint id, EntityUid originStation)
    {
        ID = id;
        OriginStation = originStation;
    }
}
