using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Payloads;
using Robust.Shared.Prototypes;

namespace Content.Shared.SurveillanceCamera;

/// <summary>
/// Response to <see cref="SurveillanceCameraConnectRequestPayload"/>
/// from the camera in order to establish the connection.
/// </summary>
public partial record struct SurveillanceCameraConnectPayload : IRoutableNetworkPayload
{
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Request to connect to a camera from the camera monitor.
/// </summary>
public partial record struct SurveillanceCameraConnectRequestPayload : IRoutableNetworkPayload
{
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Message sent periodically by an active camera monitor towards the active camera.
/// </summary>
public partial record struct SurveillanceCameraHeartbeatRequestPayload : IRoutableNetworkPayload
{
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Response from the camera to the <see cref="SurveillanceCameraHeartbeatRequestPayload"/>.
/// </summary>
public partial record struct SurveillanceCameraHeartbeatPayload : IRoutableNetworkPayload
{
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Request to get <see cref="SurveillanceCameraDataPayload"/> from all cameras on a certain subnet.
/// </summary>
public partial record struct SurveillanceCameraPingPayload : IRoutableNetworkPayload
{
    [DataField]
    public string Subnet;

    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Response to the <see cref="SurveillanceCameraPingPayload"/> request, contains info about a camera on a subnet.
/// </summary>
public partial record struct SurveillanceCameraDataPayload : IRoutableNetworkPayload
{
    [DataField]
    public string Name;

    [DataField]
    public string Subnet;

    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// Request to connect to a camera subnet.
/// </summary>
public partial record struct SurveillanceCameraSubnetConnectPayload : INetworkPayload;

/// <summary>
/// Request to disconnect from a camera subnet.
/// </summary>
public partial record struct SurveillanceCameraSubnetDisconnectPayload : INetworkPayload;

/// <summary>
/// Request to get all available subnets.
/// </summary>
public partial record struct SurveillanceCameraPingSubnetPayload : INetworkPayload;

/// <summary>
/// Response to the <see cref="SurveillanceCameraPingSubnetPayload"/>, contains the name of the available subnet.
/// </summary>
public partial record struct SurveillanceCameraSubnetDataPayload : INetworkPayload
{
    [DataField]
    public string Subnet;

    [DataField]
    public ProtoId<DeviceFrequencyPrototype> TransmitFrequency;
}
