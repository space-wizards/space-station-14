using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords.Components;

[RegisterComponent]
public sealed partial class GeneralCriminalRecordsConsoleComponent : Component
{
    public (NetEntity, uint)? ActiveKey { get; set; }
    public GeneralStationRecordsFilter? Filter { get; set; }
}