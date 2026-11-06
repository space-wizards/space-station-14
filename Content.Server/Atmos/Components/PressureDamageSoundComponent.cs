using Robust.Shared.Audio;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Plays a warning sound when this entity takes damage from <see cref="Content.Shared.Atmos.Components.DeltaPressureComponent"/>,
/// so players get audible feedback before it breaks.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PressureDamageSoundComponent : Component
{
    /// <summary>
    /// The sound to play.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("GlassCrack");

    /// <summary>
    /// Minimum time between sounds.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The earliest time at which the sound may play again.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextSoundTime;
}
