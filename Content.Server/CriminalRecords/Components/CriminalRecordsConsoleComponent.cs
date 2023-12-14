using Content.Server.CriminalRecords.Systems;
using Content.Shared.Radio;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.CriminalRecords.Components;

/// <summary>
/// A component for Criminal Record Console storing an active station record key and a currently applied filter
/// </summary>
[RegisterComponent]
[Access(typeof(CriminalRecordsConsoleSystem))]
public sealed partial class CriminalRecordsConsoleComponent : Component
{
    /// <summary>
    /// Currently active station record key.
    /// There is no station parameter as the console uses the current station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint? ActiveKey;

    /// <summary>
    /// Currently applied filter
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GeneralStationRecordsFilter? Filter;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    // TODO: remove and have either per-player access or locking like robor ui
    [DataField]
    public bool HasAccess;
}
