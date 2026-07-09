using Content.Shared.StationRecords.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.StationRecords.Components;

[Access(typeof(SharedGeneralStationRecordConsoleSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeneralStationRecordConsoleComponent : Component
{
    /// <summary>
    /// Selected crewmember record id.
    /// Station always uses the station that owns the console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint? ActiveKey;

    /// <summary>
    /// Qualities to filter a search by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public StationRecordsFilter? Filter;

    /// <summary>
    /// Whether this Records Console is able to delete entries.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanDeleteEntries;
}
