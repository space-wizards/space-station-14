using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords.Components;

[RegisterComponent]
public sealed class GeneralCriminalRecordConsoleComponent : Component
{
    public StationRecordKey? ActiveKey { get; set; }
}
