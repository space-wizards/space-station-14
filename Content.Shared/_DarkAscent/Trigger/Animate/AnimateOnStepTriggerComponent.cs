using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._DarkAscent.Trigger.Animate;

[RegisterComponent, NetworkedComponent]
public sealed partial class AnimateOnStepTriggerComponent : Component
{
    /// <summary>
    /// Sound played on animation.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Minimum timespan between animation sounds.
    /// </summary>
    [DataField("cooldown")]
    public float SoundCooldown = 0.7f;
}
