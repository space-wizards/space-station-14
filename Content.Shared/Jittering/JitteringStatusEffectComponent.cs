using Robust.Shared.GameStates;

namespace Content.Shared.Jittering;

/// <summary>
/// Jitter as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedJitteringSystem))]
public sealed partial class JitteringStatusEffectComponent : Component
{
    [DataField]
    public float Amplitude = 10f;

    [DataField]
    public float Frequency = 4f;

    /// <summary>
    /// Whether to change any existing jitter value even if they're greater than the ones we're setting.
    /// </summary>
    [DataField]
    public bool ForceValueChange;
}
