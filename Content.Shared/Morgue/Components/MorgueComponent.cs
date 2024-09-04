using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MorgueComponent : Component
{
    /// <summary>
    ///     Whether or not the morgue beeps if a living player is inside.
    /// </summary>
    [DataField, ViewVariables]
    public bool DoSoulBeep = true;

    [DataField]
    public float AccumulatedFrameTime = 0f;

    /// <summary>
    ///     The amount of time between each beep.
    /// </summary>
    [DataField, ViewVariables]
    public float BeepTime = 10f;

    [DataField, ViewVariables]
    public SoundSpecifier OccupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
