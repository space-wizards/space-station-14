namespace Content.Shared.StationRecords.Events;

/// <summary>
/// Base event for station record events.
/// </summary>
public interface IStationRecordEvent
{
    StationRecordKey Key { get; init; }

    EntityUid Station => Key.OriginStation;
}
