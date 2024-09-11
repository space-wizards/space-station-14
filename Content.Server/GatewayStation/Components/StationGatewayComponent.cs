using Content.Server.GatewayStation.Systems;

namespace Content.Server.GatewayStation.Components;

[RegisterComponent]
[Access(typeof(StationGatewaySystem))]
public sealed partial class StationGatewayComponent : Component
{
    [DataField]
    public string GateName = string.Empty;
}
