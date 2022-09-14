namespace Content.Server.StationRecords;

[RegisterComponent]
public sealed class StationRecordsComponent : Component
{
    // Every single record in this station, by key.
    // Essentially a columnar database, but I really suck
    // at implementing that so
    [ViewVariables]
    public StationRecordSet Records = new();
}
