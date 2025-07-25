using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Sound.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class EmitSoundOnCollideComponent : BaseEmitSoundComponent
{
    public static readonly TimeSpan CollideCooldown = TimeSpan.FromSeconds(0.2);

    /// <summary>
    /// Minimum velocity required for the sound to play.
    /// </summary>
    [DataField("minVelocity")]
    public float MinimumVelocity = 3f;

    /// <summary>
    /// To avoid sound spam add a cooldown to it.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSound;
}
