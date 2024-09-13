using Content.Server.GatewayStation.Systems;

namespace Content.Server.GatewayStation.Components;

[RegisterComponent]
[Access(typeof(StationGatewaySystem))]
public sealed partial class StationGatewayConsoleComponent : Component
{
    /// <summary>
    /// Selected via UI gate. Defines the behavior of the console.
    /// </summary>
    [DataField]
    public EntityUid? SelectedGate = null;

    [DataField]
    public string ChipStorageName = "storagebase";
}
