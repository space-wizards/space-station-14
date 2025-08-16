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

[Serializable, NetSerializable]
public struct CameraMarker
{
    public Vector2 Position;
    public bool Active;
    public string Address;
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
