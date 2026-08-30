using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Makes the entity play a jitter animation when triggered.
/// If TargetUser is true the user will jitter instead.
/// </summary>
/// <summary>
/// The target requires <see cref="StatusEffectsComponent"/>.
/// TODO: Convert jitter to the new status effects system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitterOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Jitteriness of the animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Amplitude = 10.0f;

    /// <summary>
    /// Frequency for jittering.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Frequency = 4.0f;

    /// <summary>
    /// For how much time to apply the effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Time = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The status effect cooldown should be refreshed (true) or accumulated (false).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Refresh;

    /// <summary>
    /// Whether to change any existing jitter value even if they're greater than the ones we're setting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceValueChange;
}

