using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Repeatedly plays a sound with a randomized delay.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SpamEmitSoundComponent : BaseEmitSoundComponent
{
    /// <summary>
    /// The time at which the next sound will play.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextSound;

    /// <summary>
    /// The minimum time in seconds between playing the sound.
    /// </summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The maximum time in seconds between playing the sound.
    /// </summary>
    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(2);

    // Always Pvs.
    /// <summary>
    /// Content of a popup message to display whenever the sound plays.
    /// </summary>
    [DataField]
    public LocId? PopUp;

    /// <summary>
    /// Whether the timer is currently running and sounds are being played.
    /// Do not set this directly, use <see cref="EmitSoundSystem.SetEnabled"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(SharedEmitSoundSystem))]
    public bool Enabled = true;
}
