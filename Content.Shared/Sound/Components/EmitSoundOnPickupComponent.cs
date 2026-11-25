using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Simple sound emitter that emits sound on entity pickup
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EmitSoundOnPickupComponent : BaseEmitSoundComponent
{
    /// <summary>
    /// The cooldown inbetween sounds
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SoundCooldown = TimeSpan.Zero;

    /// <summary>
    /// To avoid sound spam add a cooldown to it.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextSound = TimeSpan.Zero;
}
