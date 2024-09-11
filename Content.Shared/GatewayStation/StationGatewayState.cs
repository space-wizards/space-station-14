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
    public StationGatewayState()
    {

    }
}
