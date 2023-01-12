namespace Content.Client.Gravity;

[RegisterComponent]
[Access(typeof(FloatingVisualizerSystem))]
public class FloatingVisualsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public readonly float AnimationTime = 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public readonly Vector2 Offset = new(0, 0.2f);

    public readonly string AnimationKey = "gravity";
}
