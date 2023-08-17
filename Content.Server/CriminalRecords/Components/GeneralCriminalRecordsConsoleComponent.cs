using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords.Components;

[RegisterComponent]
public sealed class GeneralCriminalRecordsConsoleComponent : Component
{
    public StationRecordKey? ActiveKey { get; set; }
    public GeneralStationRecordsFilter? Filter { get; set; }
}