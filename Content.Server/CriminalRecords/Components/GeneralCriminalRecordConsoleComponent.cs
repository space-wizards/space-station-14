using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords;

[RegisterComponent]
public sealed class GeneralCriminalRecordConsoleComponent : Component
{
    public StationRecordKey? ActiveKey { get; set; }
}
