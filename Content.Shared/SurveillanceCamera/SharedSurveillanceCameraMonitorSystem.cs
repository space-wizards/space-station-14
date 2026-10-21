using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

/// <summary>
/// Camera monitor state. If the camera is null, there should be a blank
/// space where the camera is.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorUiState(
    NetEntity? activeCamera,
    HashSet<ProtoId<DeviceFrequencyPrototype>> subnets,
    DeviceAddress activeAddress,
    ProtoId<DeviceFrequencyPrototype>? activeSubnet,
    Dictionary<DeviceAddress, string> cameras
) : BoundUserInterfaceState
{
    /// <summary>
    /// The active camera on the monitor. If this is null, the part of the UI
    /// that contains the monitor should clear.
    /// </summary>
    public NetEntity? ActiveCamera { get; } = activeCamera;

    /// <summary>
    /// Currently available subnets. Does not send the entirety of the possible
    /// cameras to view because that could be really, really large.
    /// </summary>
    public HashSet<ProtoId<DeviceFrequencyPrototype>> Subnets { get; } = subnets;

    /// <summary>
    /// Currently active address.
    /// </summary>
    public DeviceAddress ActiveAddress = activeAddress;

    /// <summary>
    /// Currently active subnet.
    /// </summary>
    public ProtoId<DeviceFrequencyPrototype>? ActiveSubnet { get; } = activeSubnet;

    /// <summary>
    /// Known cameras, by address and name.
    /// </summary>
    public Dictionary<DeviceAddress, string> Cameras { get; } = cameras;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSwitchMessage(
    DeviceAddress address,
    ProtoId<DeviceFrequencyPrototype>? cameraSubnet
) : BoundUserInterfaceMessage
{
    public DeviceAddress Address { get; } = address;
    public ProtoId<DeviceFrequencyPrototype>? CameraSubnet { get; } = cameraSubnet;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSubnetRequestMessage(
    ProtoId<DeviceFrequencyPrototype> subnet
) : BoundUserInterfaceMessage
{
    public ProtoId<DeviceFrequencyPrototype> Subnet { get; } = subnet;
}

/// <summary>
/// Sent when the user requests that the cameras on the current subnet be refreshed.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshCamerasMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent when the user requests that the subnets known by the monitor be refreshed.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshSubnetsMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent when the user wants to disconnect the monitor from the camera.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurveillanceCameraDisconnectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum SurveillanceCameraMonitorUiKey : byte
{
    Key
}

#region SETUP

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupBoundUiState(
    string name,
    uint network,
    List<ProtoId<DeviceFrequencyPrototype>> networks,
    bool nameDisabled,
    bool networkDisabled
) : BoundUserInterfaceState
{
    public string Name { get; } = name;
    public uint Network { get; } = network;
    public List<ProtoId<DeviceFrequencyPrototype>> Networks { get; } = networks;
    public bool NameDisabled { get; } = nameDisabled;
    public bool NetworkDisabled { get; } = networkDisabled;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupSetName(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupSetNetwork(int network) : BoundUserInterfaceMessage
{
    public int Network { get; } = network;
}

[Serializable, NetSerializable]
public enum SurveillanceCameraSetupUiKey : byte
{
    Camera,
    Router
}

#endregion
