using Content.Shared._CD.Records;
using Content.Shared.StationRecords;
using Robust.Shared.GameObjects;

namespace Content.Server._CD.Records.Consoles;

/// <summary>
/// BUI source for the record consoles on stations.
/// </summary>
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
