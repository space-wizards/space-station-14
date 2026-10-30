using Content.Shared.StationRecords.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.StationRecords.Components;

[Access(typeof(StationRecordsSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class StationRecordsComponent : Component
{
    // Every single record in this station, by key.
    // Essentially a columnar database, but I really suck
    // at implementing that so
    [IncludeDataField, AutoNetworkedField]
    public StationRecordSet Records = new();
}
