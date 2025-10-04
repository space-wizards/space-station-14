using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// When added to an entity storage this component will keep track of the mind status of the player inside.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MorgueComponent : Component
{
    /// <summary>
    /// Whether or not the morgue beeps if a living player is inside.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoSoulBeep = true;

    /// <summary>
    /// The timestamp for the next beep.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextBeep = TimeSpan.Zero;

    /// <summary>
    /// The amount of time between each beep.
    /// </summary>
    [DataField]
    public TimeSpan BeepTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The beep sound to play.
    /// </summary>
    [DataField]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
