using Robust.Client.Animations;

[RegisterComponent]
public sealed class VaporVisualsComponent : Component
{
    public string AnimationKey = "flick_animation";

    [DataField("animation_time")]
    public float Delay = 0.25f;

    [DataField("animation_state")]
    public string State = "chempuff";

    public Animation VaporFlick = default!;
}
