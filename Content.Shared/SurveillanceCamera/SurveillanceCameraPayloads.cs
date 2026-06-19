using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraConnectPayload : NetworkPayload
{
    [DataField]
    public string Address;
}

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetConnectPayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetDisconnectPayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraHeartbeatPayload : NetworkPayload
{
    [DataField]
    public string Address;
}

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraPingPayload : NetworkPayload
{
    [DataField]
    public string Subnet;
}

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraPingSubnetPayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraDataPayload : NetworkPayload
{
    [DataField]
    public string Name;

    [DataField]
    public string Subnet;

    [DataField]
    public string Address;
}

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetDataPayload : NetworkPayload
{
    [DataField]
    public string Subnet;
}
