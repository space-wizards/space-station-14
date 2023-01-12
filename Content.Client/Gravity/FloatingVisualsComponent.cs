namespace Content.Client.Gravity;

[RegisterComponent]
[Access(typeof(FloatingVisualizerSystem))]
public class FloatingVisualsComponent : Component
{
    [ViewVariables]
    public readonly float AnimationTime = 2f;

    [ViewVariables]
    public readonly Vector2 Offset = new(0, 0.2f);

    public readonly string AnimationKey = "gravity";
}
