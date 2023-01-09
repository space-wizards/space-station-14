using Robust.Client.Animations;

namespace Content.Client.Gravity;

[RegisterComponent]
[Access(typeof(NoGravityVisualizerSystem))]
public class NoGravityVisualsComponent : Component
{
    [ViewVariables]
    public readonly float AnimationTime = 2f;

    [ViewVariables]
    public readonly Vector2 Offset = new(0, 0.2f);

    [ViewVariables]
    public bool Enabled;

    [ViewVariables]
    public Animation Animation = default!;

    public readonly string AnimationKey = "gravity";
}
