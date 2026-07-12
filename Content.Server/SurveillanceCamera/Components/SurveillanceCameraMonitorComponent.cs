namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
[Access(typeof(SurveillanceCameraMonitorSystem))]
public sealed partial class SurveillanceCameraMonitorComponent : Component
{
    /// <summary>
    /// Currently active camera viewed by this monitor.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActiveCamera { get; set; }

    [ViewVariables]
    public string ActiveCameraAddress { get; set; } = string.Empty;

    /// <summary>
    /// Last time this monitor was sent a heartbeat.
    /// </summary>
    [ViewVariables]
    public float LastHeartbeat { get; set; }

    /// <summary>
    /// Last time this monitor sent a heartbeat.
    /// </summary>
    [ViewVariables]
    public float LastHeartbeatSent { get; set; }

    /// <summary>
    /// Next camera this monitor is trying to connect to.
    /// If the monitor has connected to the camera, this should be set to null.
    /// </summary>
    [ViewVariables]
    public string? NextCameraAddress { get; set; }

    /// <summary>
    /// Set of viewers currently looking at this monitor.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Viewers { get; } = new();

    /// <summary>
    /// Current active subnet.
    /// </summary>
    [ViewVariables]
    public string ActiveSubnet { get; set; } = default!;

    /// <summary>
    /// Known cameras in this subnet by address with name values.
    /// This is cleared when the subnet is changed.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownCameras { get; } = new();

    /// <summary>
    /// The subnets known by this camera monitor.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownSubnets { get; } = new();
}
