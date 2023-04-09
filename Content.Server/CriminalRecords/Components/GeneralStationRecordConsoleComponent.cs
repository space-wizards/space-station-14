using Content.Shared.StationRecords;

namespace Content.Server.CriminalnRecords;

[RegisterComponent]
public sealed class GeneralCriminalRecordConsoleComponent : Component
{
    public StationRecordKey? ActiveKey { get; set; }
}
