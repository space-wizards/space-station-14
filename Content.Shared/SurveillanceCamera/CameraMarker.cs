using System.Numerics;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

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
    public DeviceAddress Address;

    /// <summary>
    /// Subnet the camera is connected to.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype> Subnet;

    /// <summary>
    /// Should the camera be displayed on the camera map.
    /// </summary>
    [DataField]
    public bool Visible = true;
}
