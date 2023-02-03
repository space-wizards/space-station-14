using Robust.Client.Animations;
using Robust.Shared.Audio;

namespace Content.Client.Trigger;

[RegisterComponent]
[Access(typeof(TimerTriggerVisualizerSystem))]
public sealed class TimerTriggerVisualizerComponent : Component
{
    public const string AnimationKey = "priming_animation";

    [DataField("countdown_sound")]
    public SoundSpecifier? CountdownSound;

    public Animation PrimingAnimation = default!;
}
