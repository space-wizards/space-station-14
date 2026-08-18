using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

// Camera monitor state. If the camera is null, there should be a blank
// space where the camera is.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorUiState(NetEntity? activeCamera,
    HashSet<ProtoId<DeviceFrequencyPrototype>> subnets,
    string activeAddress,
    ProtoId<DeviceFrequencyPrototype>? activeSubnet,
    Dictionary<string, string> cameras
) : BoundUserInterfaceState
{
    // The active camera on the monitor. If this is null, the part of the UI
    // that contains the monitor should clear.
    public NetEntity? ActiveCamera { get; } = activeCamera;

    // Currently available subnets. Does not send the entirety of the possible
    // cameras to view because that could be really, really large
    public HashSet<ProtoId<DeviceFrequencyPrototype>> Subnets { get; } = subnets;

    public string ActiveAddress = activeAddress;

    // Currently active subnet.
    public ProtoId<DeviceFrequencyPrototype>? ActiveSubnet { get; } = activeSubnet;

    // Known cameras, by address and name.
    public Dictionary<string, string> Cameras { get; } = cameras;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSwitchMessage(
    string address,
    ProtoId<DeviceFrequencyPrototype>? cameraSubnet
) : BoundUserInterfaceMessage
{
    public string Address { get; } = address;
    public ProtoId<DeviceFrequencyPrototype>? CameraSubnet { get; } = cameraSubnet;
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSubnetRequestMessage(
    ProtoId<DeviceFrequencyPrototype> subnet
) : BoundUserInterfaceMessage
{
    public ProtoId<DeviceFrequencyPrototype> Subnet { get; } = subnet;
}

// Sent when the user requests that the cameras on the current subnet be refreshed.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshCamerasMessage : BoundUserInterfaceMessage;

// Sent when the user requests that the subnets known by the monitor be refreshed.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshSubnetsMessage : BoundUserInterfaceMessage;

// Sent when the user wants to disconnect the monitor from the camera.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraDisconnectMessage : BoundUserInterfaceMessage;
[Serializable, NetSerializable]
public enum SurveillanceCameraMonitorUiKey : byte
{
    Key
}

// SETUP

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
