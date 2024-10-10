using Robust.Shared.GameStates;

namespace Content.Shared.GatewayStation;

/// <summary>
/// Console that allows you to manage the StationGatewayComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStationGatewaySystem))]
public sealed partial class StationGatewayConsoleComponent : Component
{
    /// <summary>
    /// Selected via UI gate. Defines the behavior of the console.
    /// </summary>
    [DataField]
    public EntityUid? SelectedGate;

    /// <summary>
    /// Portals created by this console will be colored in the specified color. This can be used to make Syndicate portals blood red.
    /// </summary>
    [DataField]
    public Color GatewayColor = Color.White;

    /// <summary>
    /// A storage from which all coordinate chips are scanned
    /// </summary>
    [DataField]
    public string ChipStorageName = "storagebase";

    [DataField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1f);

}
