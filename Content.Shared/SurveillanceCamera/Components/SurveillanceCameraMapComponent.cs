using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurveillanceCameraMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, CameraMarker> Cameras = new();
}

[Serializable, NetSerializable, DataDefinition]
public partial struct CameraMarker
{
    [DataField]
    public Vector2 Position;

    [DataField]
    public bool Active;

    [DataField]
    public string Address;

    [DataField]
    public string Subnet;
}

[Serializable, NetSerializable]
public sealed class RequestCameraMarkerUpdateMessage : EntityEventArgs
{
    public NetEntity CameraEntity { get; }

    public RequestCameraMarkerUpdateMessage(NetEntity cameraEntity)
    {
        CameraEntity = cameraEntity;
    }
}
