namespace Content.Client.Rotation;

[RegisterComponent]
public sealed class RotationVisualsComponent : Component
{
    public static readonly Angle DefaultRotation = Angle.FromDegrees(90);

    [ViewVariables(VVAccess.ReadWrite)]
    public Angle VerticalRotation = 0;

    [ViewVariables(VVAccess.ReadWrite)] public Angle HorizontalRotation = DefaultRotation;

    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationTime = 0.125f;
}
