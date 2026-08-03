using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

// Camera monitor state. If the camera is null, there should be a blank
// space where the camera is.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorUiState : BoundUserInterfaceState
{
    // The active camera on the monitor. If this is null, the part of the UI
    // that contains the monitor should clear.
    public readonly NetEntity? ActiveCamera;

    // Currently available subnets. Does not send the entirety of the possible
    // cameras to view because that could be really, really large
    public readonly HashSet<DeviceFrequency> Subnets;

    public readonly DeviceAddress ActiveAddress;

    // Currently active subnet.
    public readonly DeviceFrequency? ActiveSubnet;

    // Known cameras, by address and name.
    public readonly Dictionary<DeviceAddress, string> Cameras;

    public SurveillanceCameraMonitorUiState(NetEntity? activeCamera, HashSet<DeviceFrequency> subnets, DeviceAddress activeAddress, DeviceFrequency? activeSubnet, Dictionary<DeviceAddress, string> cameras)
    {
        ActiveCamera = activeCamera;
        Subnets = subnets;
        ActiveAddress = activeAddress;
        ActiveSubnet = activeSubnet;
        Cameras = cameras;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSwitchMessage : BoundUserInterfaceMessage
{
    public readonly DeviceAddress Address;
    public readonly DeviceFrequency? CameraSubnet;

    public SurveillanceCameraMonitorSwitchMessage(DeviceAddress address, DeviceFrequency? cameraSubnet = null)
    {
        Address = address;
        CameraSubnet = cameraSubnet;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSubnetRequestMessage : BoundUserInterfaceMessage
{
    public readonly DeviceFrequency Subnet;

    public SurveillanceCameraMonitorSubnetRequestMessage(DeviceFrequency subnet)
    {
        Subnet = subnet;
    }
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
public sealed class SurveillanceCameraSetupBoundUiState : BoundUserInterfaceState
{
    public readonly string Name;
    public readonly uint Network;
    public readonly List<ProtoId<DeviceFrequencyPrototype>> Networks;
    public readonly bool NameDisabled;
    public readonly bool NetworkDisabled;

    public SurveillanceCameraSetupBoundUiState(string name, uint network, List<ProtoId<DeviceFrequencyPrototype>> networks, bool nameDisabled, bool networkDisabled)
    {
        Name = name;
        Network = network;
        Networks = networks;
        NameDisabled = nameDisabled;
        NetworkDisabled = networkDisabled;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupSetName : BoundUserInterfaceMessage
{
    public readonly string Name;

    public SurveillanceCameraSetupSetName(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupSetNetwork : BoundUserInterfaceMessage
{
    public readonly int Network;

    public SurveillanceCameraSetupSetNetwork(int network)
    {
        Network = network;
    }
}


[Serializable, NetSerializable]
public enum SurveillanceCameraSetupUiKey : byte
{
    Camera,
    Router
}
