using Content.Shared._CD.Records;
using Content.Shared.StationRecords;

namespace Content.Server._CD.Records.Consoles;

[RegisterComponent]
public sealed partial class CharacterRecordConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public uint? SelectedIndex { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public StationRecordsFilter? Filter;

    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public RecordConsoleType ConsoleType;
}
