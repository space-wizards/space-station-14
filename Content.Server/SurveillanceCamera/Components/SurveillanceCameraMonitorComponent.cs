namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
[Access(typeof(SurveillanceCameraMonitorSystem))]
public sealed class SurveillanceCameraMonitorComponent : Component
{
    // Currently active camera viewed by this monitor.
    public EntityUid? ActiveCamera { get; set; }
    public string ActiveCameraAddress { get; set; } = string.Empty;

    // Last time this monitor was sent a heartbeat.
    public float LastHeartbeat { get; set; }
    // Last time this monitor sent a heartbeat.
    public float LastHeartbeatSent { get; set; }

    // Next camera this monitor is trying to connect to.
    // If the monitor has connected to the camera, this
    // should be set to null.
    public string? NextCameraAddress { get; set; }

    [ViewVariables]
    // Set of viewers currently looking at this monitor.
    public HashSet<EntityUid> Viewers { get; } = new();

    // Current active subnet.
    public string ActiveSubnet { get; set; } = default!;

    // Known cameras in this subnet by address with name values.
    // This is cleared when the subnet is changed.
    public Dictionary<string, string> KnownCameras { get; } = new();

    [ViewVariables]
    // The subnets known by this camera monitor.
    public Dictionary<string, string> KnownSubnets { get; } = new();
}
