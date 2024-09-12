using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GatewayStation;

[Serializable, NetSerializable]
public enum StationGatewayUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class StationGatewayState : BoundUserInterfaceState
{
    public List<StationGatewayStatus> Gateways;
    public StationGatewayState(List<StationGatewayStatus> gateways)
    {
        Gateways = gateways;
    }
}

[Serializable, NetSerializable]
public sealed class StationGatewayStatus
{
    public StationGatewayStatus(NetEntity gatewayUid, NetCoordinates coordinates, NetCoordinates? link, string name)
    {
        GatewayUid = gatewayUid;
        Coordinates = coordinates;
        LinkCoordinates = link;
        Name = name;
    }

    public NetEntity GatewayUid;
    public NetCoordinates? Coordinates;
    public NetCoordinates? LinkCoordinates;
    public string Name;
}
