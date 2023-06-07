using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components;

/// <summary>
///     Component for tracking active trigger timers. A timers can activated by some other component, e.g. <see cref="OnUseTimerTriggerComponent"/>.
/// </summary>
[RegisterComponent]
public sealed class ActiveTimerTriggerComponent : Component
{
    [DataField("timeRemaining")]
    public float TimeRemaining;

    [DataField("user")]
    public EntityUid? User;

    [DataField("beepInterval")]
    public float BeepInterval;

    [DataField("timeUntilBeep")]
    public float TimeUntilBeep;

    [DataField("beepSound")]
    public SoundSpecifier? BeepSound;
}
