using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components;
// move to shared probably.
[RegisterComponent]
public sealed partial class RadarTrackedComponent : Component
{
    public RadarSignatureShape Shape = RadarSignatureShape.Circle;

    /// <summary>
    /// Color of the indicator on the nav ui
    /// </summary>
    [DataField]
    public Color RadarColor = Color.FromSrgb(Color.Red);

    /// <summary>
    /// Rotation of the indicator on the nav ui
    /// </summary>
    [DataField]
    public Angle RadarAngle = Angle.Zero;

    /// <summary>
    /// Size of the major axis of the indicator on the nav ui
    /// </summary>
    [DataField]
    public float Size = 1.0f;
}

[Serializable, NetSerializable]
public enum RadarSignatureShape : byte
{
    Circle,
    Square,
    Triangle,
    Diamond,
    Chevron,
    Line
}
