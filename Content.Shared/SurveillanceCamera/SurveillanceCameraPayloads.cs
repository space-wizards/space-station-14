using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Payloads;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

/// <summary>
/// Response to <see cref="SurveillanceCameraConnectRequestPayload"/>
/// from the camera in order to establish the connection.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraConnectPayload : RoutableNetworkPayload;

/// <summary>
/// Request to connect to a camera from the camera monitor.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraConnectRequestPayload : RoutableNetworkPayload;

/// <summary>
/// Message sent periodically by an active camera monitor towards the active camera.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraHeartbeatRequestPayload : RoutableNetworkPayload;

/// <summary>
/// Response from the camera to the <see cref="SurveillanceCameraHeartbeatRequestPayload"/>.
/// </summary>
public sealed partial class SurveillanceCameraHeartbeatPayload : RoutableNetworkPayload;

/// <summary>
/// Request to get <see cref="SurveillanceCameraDataPayload"/> from all cameras on a certain subnet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraPingPayload : RoutableNetworkPayload
{
    [DataField]
    public string Subnet;
}

/// <summary>
/// Response to the <see cref="SurveillanceCameraPingPayload"/> request, contains info about a camera on a subnet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraDataPayload : RoutableNetworkPayload
{
    [DataField]
    public string Name;

    [DataField]
    public string Subnet;
}

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetConnectPayload : HandledNetworkPayload;

[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetDisconnectPayload : HandledNetworkPayload;

/// <summary>
/// Request to get all available subnets.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraPingSubnetPayload : HandledNetworkPayload;

/// <summary>
/// Response to the <see cref="SurveillanceCameraPingSubnetPayload"/>, contains the name of the available subnet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SurveillanceCameraSubnetDataPayload : HandledNetworkPayload
{
    [DataField]
    public string Subnet;
}
