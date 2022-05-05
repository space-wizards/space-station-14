namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorComponent : Component
{
    // Currently active camera viewed by this monitor.
    public EntityUid? ActiveCamera { get; set; }

    // Next camera this monitor is trying to connect to.
    // If the monitor has connected to the camera, this
    // should be set to null.
    public string? NextCameraAddress { get; set; }

    // Set of viewers currently looking at this monitor.
    public HashSet<EntityUid> Viewers { get; } = new();

    // The subnets known by this camera monitor.
    public List<string> KnownSubnets { get; } = new();
}
