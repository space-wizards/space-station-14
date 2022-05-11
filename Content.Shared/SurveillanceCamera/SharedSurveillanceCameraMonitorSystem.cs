using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

// Camera monitor state. If the camera is null, there should be a blank
// space where the camera is.
//
// Server side, whenever a camera is disabled, it should raise an event
// that eventually leads to anything actively monitoring that camera
// (so, any monitors) getting a null camera in this UI state.
//
// Monitors should store any entities currently viewing it,
// Cameras should store any monitors currently showing it.
//
// When a monitor has somebody move away from it, it should
// remove the user from its active viewers, which in turn
// removes the user from entities actively looking through
// the camera's eye
//
// Which should be authorative, the camera monitor or the camera itself?
//
// Camera monitor would have a list of viewers and an active camera,
// where the monitor system manages the view subscriptions
//
// Camera would have a list of viewers, where the camera system
// manages the view subscriptions.
//
// I think that SurveillanceCameraComponent should be an object
// that stores information about the camera as well as an eye storage
// (but this could just be replaced with an EyeComponent tbh)
[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorUiState : BoundUserInterfaceState
{
    // The active camera on the monitor. If this is null, the part of the UI
    // that contains the monitor should clear.
    public EntityUid? ActiveCamera { get; }

    // Currently available subnets. Does not send the entirety of the possible
    // cameras to view because that could be really, really large
    public HashSet<string> Subnets { get; }

    public SurveillanceCameraMonitorUiState(EntityUid? activeCamera, HashSet<string> subnets)
    {
        ActiveCamera = activeCamera;
        Subnets = subnets;
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

[Serializable, NetSerializable]
public sealed class SurveillanceCameraMonitorInfoMessage : BoundUserInterfaceMessage
{
    public SurveillanceCameraInfo SubnetInfo { get; }

    public SurveillanceCameraMonitorInfoMessage(SurveillanceCameraInfo subnetInfo)
    {
        SubnetInfo = subnetInfo;
    }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraInfo
{
    public string Address { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Subnet { get; set; } = default!;
}

[Serializable, NetSerializable]
public enum SurveillanceCameraMonitorUiKey : byte
{
    Key
}
