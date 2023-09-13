namespace Content.Client.Rotation;

[RegisterComponent]
public sealed partial class RotationVisualsComponent : Component
{
    [DataField("defaultRotation")]
    [ViewVariables(VVAccess.ReadOnly)]
    public Angle DefaultRotation = Angle.FromDegrees(90);

    [ViewVariables(VVAccess.ReadWrite)]
    public Angle VerticalRotation = 0;

    [DataField("horizontalRotation")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle HorizontalRotation = Angle.FromDegrees(90);

    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationTime = 0.125f;
}
