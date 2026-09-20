namespace Content.Shared.StationRecords.Events;

/// <summary>
///     Event raised after a record is modified. This is to
///     inform other systems that records stored in this key
///     may have changed.
/// </summary>
[ByRefEvent]
public readonly record struct RecordModifiedEvent(StationRecordKey Key) : IStationRecordEvent
{
    public EntityUid Station => Key.OriginStation;
}
