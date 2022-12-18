namespace Content.Server.StationRecords;

[Access(typeof(StationRecordsSystem))]
[RegisterComponent]
public sealed class StationRecordsComponent : Component
{
    // Every single record in this station, by key.
    // Essentially a columnar database, but I really suck
    // at implementing that so
    [ViewVariables] public readonly StationRecordSet Records = new();
}
