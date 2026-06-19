using Content.Shared.DeviceNetwork;

namespace Content.Shared.SurveillanceCamera;

public sealed partial class SurveillanceCameraConnectPayload : NetworkPayload
{
    [DataField]
    public string Address;
}

public sealed partial class SurveillanceCameraSubnetConnectPayload : NetworkPayload
{

}

public sealed partial class SurveillanceCameraSubnetDisconnectPayload : NetworkPayload
{

}

public sealed partial class SurveillanceCameraHeartbeatPayload : NetworkPayload
{
    [DataField]
    public string Address;
}

public sealed partial class SurveillanceCameraPingPayload : NetworkPayload
{
    [DataField]
    public string Subnet;
}

public sealed partial class SurveillanceCameraPingSubnetPayload : NetworkPayload
{

}

public sealed partial class SurveillanceCameraDataPayload : NetworkPayload
{
    [DataField]
    public string Name;

    [DataField]
    public string Subnet;

    [DataField]
    public string Address;
}

public sealed partial class SurveillanceCameraSubnetDataPayload : NetworkPayload
{
    [DataField]
    public string Subnet;
}
