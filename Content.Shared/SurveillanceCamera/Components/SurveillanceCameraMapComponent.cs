using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera.Components;

/// <summary>
/// Stores surveillance camera data for the camera map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurveillanceCameraMapComponent : Component
{
    /// <summary>
    /// Dictionary of cameras on on the current grid.
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<NetEntity, CameraMarker> Cameras = new();
}

/// <summary>
/// Represents a camera marker on the camera map.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial struct CameraMarker
{
    /// <summary>
    /// Position of the camera in local map coordinates.
    /// </summary>
    [DataField]
    public Vector2 Position;

    /// <summary>
    /// Whether the camera is active.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// Network address of the camera.
    /// </summary>
    [DataField]
    public string Address;

    /// <summary>
    /// Subnet the camera is connected to.
    /// </summary>
    [DataField]
    public string Subnet;

    /// <summary>
    /// Should the camera be displayed on the camera map.
    /// </summary>
    [DataField]
    public bool Visible = true;
}

/// <summary>
/// Network event for requesting camera marker updates.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestCameraMarkerUpdateMessage(NetEntity cameraEntity) : EntityEventArgs
{
    public NetEntity CameraEntity { get; } = cameraEntity;
}
