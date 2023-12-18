using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Explosion.Components;

/// <summary>
///     Component for tracking active trigger timers. A timers can activated by some other component, e.g. <see cref="OnUseTimerTriggerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveTimerTriggerComponent : Component
{
    [DataField("timeRemaining")] public float TimeRemaining;

    [DataField("user")] public EntityUid? User;

    [DataField("beepInterval")] public float BeepInterval;

    [DataField("timeUntilBeep")] public float TimeUntilBeep;

    [DataField("beepSound")] public SoundSpecifier? BeepSound;
}
