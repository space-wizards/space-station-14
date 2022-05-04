using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

public abstract class SharedSurveillanceCameraComponent : Component
{
    // Clients should derive this and add the IEye field,
    // servers need the DeviceList so that they can deal
    // with devicenet

    // Standard eye fields.
    public Vector2 Offset { get; set; }
    public Angle Rotation { get; set; }
    public Vector2 Zoom { get; set; }
    public Vector2 Scale { get; set; }
}

[Serializable, NetSerializable]
public sealed class SurveillanceCameraComponentState : ComponentState
{
    public Vector2 Offset { get; set; }
    public Angle Rotation { get; set; }
    public Vector2 Zoom { get; set; }
    public Vector2 Scale { get; set; }
}
