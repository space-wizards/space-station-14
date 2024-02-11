using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MorgueComponent : Component
{
    /// <summary>
    ///     Whether or not the morgue beeps if a living player is inside.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("doSoulBeep")]
    public bool DoSoulBeep = true;

    [ViewVariables]
    public float AccumulatedFrameTime = 0f;

    /// <summary>
    ///     The amount of time between each beep.
    /// </summary>
    [ViewVariables]
    public float BeepTime = 10f;

    [DataField("occupantHasSoulAlarmSound")]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
