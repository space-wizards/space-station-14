using Robust.Client.Animations;

namespace Content.Client.Chemistry.Visualizers;

[RegisterComponent]
[Access(typeof(VaporVisualizerSystem))]
public sealed class VaporVisualizerComponent : Component
{
    public const string AnimationKey = "flick_animation";

    [DataField("animation_time")]
    public float Delay = 0.25f;

    [DataField("animation_state")]
    public string State = "chempuff";

    [ViewVariables(VVAccess.ReadOnly)]
    public Animation VaporFlick = default!;
}
