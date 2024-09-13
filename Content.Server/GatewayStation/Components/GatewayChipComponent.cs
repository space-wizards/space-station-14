using Content.Server.GatewayStation.Systems;

namespace Content.Server.GatewayStation.Components;

/// <summary>
/// Stores a reference to a specific Gateway. Can be inserted into the gateway control console so that the console can control this gateway
/// </summary>
[RegisterComponent]
[Access(typeof(StationGatewaySystem))]
public sealed partial class GatewayChipComponent : Component
{
    [DataField]
    public EntityUid? ConnectedGate;
}
