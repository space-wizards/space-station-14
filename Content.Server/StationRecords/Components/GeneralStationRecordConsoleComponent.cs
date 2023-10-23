using Content.Shared.StationRecords;

namespace Content.Server.StationRecords;

[RegisterComponent]
public sealed partial class GeneralStationRecordConsoleComponent : Component
{
    public (NetEntity, uint)? ActiveKey { get; set; }
    public GeneralStationRecordsFilter? Filter { get; set; }
}
