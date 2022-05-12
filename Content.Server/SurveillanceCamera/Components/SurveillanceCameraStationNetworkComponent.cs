namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed class SurveillanceCameraStationNetworkComponent : Component
{
    // Network of cameras. EntityUids are stored here in sets via strings that
    // represent user-settable subnets. Subnets shouldn't really be set from
    // any game client, and instead should be set via a prototype...
    public Dictionary<string, HashSet<EntityUid>> CameraSubnets { get; } = new();
}
