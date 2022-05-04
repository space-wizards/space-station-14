namespace Content.Server.SurveillanceCamera;

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
public sealed class SurveillanceCameraMonitorUiState : BoundUserInterfaceState
{
    // The active camera on the monitor. If this is null, the part of the UI
    // that contains the monitor should clear.
    public EntityUid? ActiveCamera { get; }

    // Currently available subnets. Does not send the entirety of the possible
    // cameras to view because that could be really, really large
    public List<string> Subnets { get; }

    public SurveillanceCameraMonitorUiState(EntityUid? activeCamera, List<string> subnets)
    {
        ActiveCamera = activeCamera;
        Subnets = subnets;
    }
}

public enum SurveillanceCameraMonitorUiKey : byte
{
    Key
}
