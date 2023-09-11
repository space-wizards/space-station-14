using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

// Camera monitor state. If the camera is null, there should be a blank
// space where the camera is.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorUiState : BoundUserInterfaceState
{
    // The active camera on the monitor. If this is null, the part of the UI
    // that contains the monitor should clear.
    public NetEntity? ActiveCamera { get; }

    // Currently available subnets. Does not send the entirety of the possible
    // cameras to view because that could be really, really large
    public HashSet<string> Subnets { get; }

    public string ActiveAddress;

    // Currently active subnet.
    public string ActiveSubnet { get; }

    // Known cameras, by address and name.
    public Dictionary<string, string> Cameras { get; }

    public SurveillanceCameraMonitorUiState(NetEntity? activeCamera, HashSet<string> subnets, string activeAddress, string activeSubnet, Dictionary<string, string> cameras)
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
    public string Address { get; }

    public SurveillanceCameraMonitorSwitchMessage(string address)
    {
        Address = address;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorSubnetRequestMessage : BoundUserInterfaceMessage
{
    public string Subnet { get; }

    public SurveillanceCameraMonitorSubnetRequestMessage(string subnet)
    {
        Subnet = subnet;
    }
}

// Sent when the user requests that the cameras on the current subnet be refreshed.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshCamerasMessage : BoundUserInterfaceMessage
{}

// Sent when the user requests that the subnets known by the monitor be refreshed.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraRefreshSubnetsMessage : BoundUserInterfaceMessage
{}

// Sent when the user wants to disconnect the monitor from the camera.
[Serializable, NetSerializable]
public sealed class SurveillanceCameraDisconnectMessage : BoundUserInterfaceMessage
{}

[Serializable, NetSerializable]
public enum SurveillanceCameraMonitorUiKey : byte
{
    Key
}

// SETUP

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupBoundUiState : BoundUserInterfaceState
{
    public string Name { get; }
    public uint Network { get; }
    public List<string> Networks { get; }
    public bool NameDisabled { get; }
    public bool NetworkDisabled { get; }

    public SurveillanceCameraSetupBoundUiState(string name, uint network, List<string> networks, bool nameDisabled, bool networkDisabled)
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
    public string Name { get; }

    public SurveillanceCameraSetupSetName(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraSetupSetNetwork : BoundUserInterfaceMessage
{
    public int Network { get; }

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
