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
    public NetEntity? SelectedGateway;
    public List<StationGatewayStatus> Gateways;
    public StationGatewayState(List<StationGatewayStatus> gateways, NetEntity? selected = null)
    {
        Gateways = gateways;
        SelectedGateway = selected;
    }
}

[Serializable, NetSerializable]
public sealed class StationGatewayStatus
{
    public StationGatewayStatus(NetEntity gatewayUid, NetCoordinates coordinates, NetEntity gatewayLinkedUid, NetCoordinates? link, string name, bool powered)
    {
        GatewayUid = gatewayUid;
        Coordinates = coordinates;
        GatewayLinkedUid = gatewayLinkedUid;
        LinkCoordinates = link;
        Name = name;
        Powered = powered;
    }

    public NetEntity GatewayUid;
    public NetEntity GatewayLinkedUid;
    public NetCoordinates? Coordinates;
    public NetCoordinates? LinkCoordinates;
    public string Name;
    public bool Powered;
}

[Serializable, NetSerializable]
public sealed class StationGatewayGateClickMessage : BoundUserInterfaceMessage
{
    public NetEntity? Gateway;

    /// <summary>
    /// Called when the client clicks on any active Gateway on the StationGatewayConsoleComponent
    /// </summary>
    public StationGatewayGateClickMessage(NetEntity? gateway)
    {
        Gateway = gateway;
    }
}

[Serializable, NetSerializable]
public enum GatewayPortalVisual
{
    Color,
}
