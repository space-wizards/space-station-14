using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;

namespace Content.Server.StationRecords;

[RegisterComponent, Access(typeof(GeneralStationRecordConsoleSystem))]
public sealed partial class GeneralStationRecordConsoleComponent : Component
{
    /// <summary>
    /// Selected crewmember record id.
    /// Station always uses the station that owns the console.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint? ActiveKey;

    /// <summary>
    /// Qualitites to filter a search by.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GeneralStationRecordsFilter? Filter;
}
