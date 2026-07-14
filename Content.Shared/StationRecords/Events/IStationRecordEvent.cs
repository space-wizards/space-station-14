namespace Content.Shared.StationRecords.Events;

/// <summary>
/// Base event for station record events.
/// </summary>
public interface IStationRecordEvent
{
    StationRecordKey Key { get; set; }

    EntityUid Station => Key.OriginStation;
}
