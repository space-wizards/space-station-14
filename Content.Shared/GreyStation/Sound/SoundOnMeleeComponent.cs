using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GreyStation.Sound;

/// <summary>
/// Plays a sound when you hit something.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SoundOnMeleeSystem))]
[AutoGenerateComponentPause]
public sealed partial class SoundOnMeleeComponent : Component
{
    /// <summary>
    /// Sound played after hitting something
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier? Sound;

    /// <summary>
    /// How long to wait before another sound can be played.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When a sound can be played again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSound = TimeSpan.Zero;
}
