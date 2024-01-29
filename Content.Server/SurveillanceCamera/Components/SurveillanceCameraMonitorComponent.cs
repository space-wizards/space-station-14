namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
[Access(typeof(SurveillanceCameraMonitorSystem))]
public sealed partial class SurveillanceCameraMonitorComponent : Component
{
    // Currently active camera viewed by this monitor.
    [ViewVariables]
    public EntityUid? ActiveCamera { get; set; }

    [ViewVariables]
    public string ActiveCameraAddress { get; set; } = string.Empty;

    [ViewVariables]
    // Last time this monitor was sent a heartbeat.
    public float LastHeartbeat { get; set; }

    [ViewVariables]
    // Last time this monitor sent a heartbeat.
    public float LastHeartbeatSent { get; set; }

    // Next camera this monitor is trying to connect to.
    // If the monitor has connected to the camera, this
    // should be set to null.
    [ViewVariables]
    public string? NextCameraAddress { get; set; }

    [ViewVariables]
    // Set of viewers currently looking at this monitor.
    public HashSet<EntityUid> Viewers { get; } = new();

    // Current active subnet.
    [ViewVariables]
    public string ActiveSubnet { get; set; } = default!;

    // Known cameras in this subnet by address with name values.
    // This is cleared when the subnet is changed.
    [ViewVariables]
    public Dictionary<string, string> KnownCameras { get; } = new();

    [ViewVariables]
    // The subnets known by this camera monitor.
    public Dictionary<string, string> KnownSubnets { get; } = new();
}
