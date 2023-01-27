using Content.Shared.StationRecords;

namespace Content.Server.StationRecords;

[RegisterComponent]
public sealed partial class GeneralStationRecordConsoleComponent : Component
{
    public StationRecordKey? ActiveKey { get; set; }
}
