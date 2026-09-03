namespace Content.Shared.StationRecords.Events;

/// <summary>
///     Event raised after a record is removed. Only the key is given
///     when the record is removed, so that any relevant systems/components
///     that store record keys can then remove the key from their internal
///     fields.
/// </summary>
[ByRefEvent]
public readonly record struct RecordRemovedEvent(StationRecordKey Key) : IStationRecordEvent
{
    public EntityUid Station => Key.OriginStation;
}
