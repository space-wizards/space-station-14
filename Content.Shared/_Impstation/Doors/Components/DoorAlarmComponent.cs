using Content.Shared.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles bolt-specific behavior.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoorSystem))]
[AutoGenerateComponentState]
public sealed partial class DoorAlarmComponent : Component
{
    /// <summary>
    /// Whether the alarm is currently tripped
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlarmTripped = false;

    /// <summary>
    /// True if the Alarm wire is cut, which will disable it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlarmWireCut;
    /// <summary>
    /// Alarm sound
    /// </summary>
    [DataField]
    public SoundSpecifier AlarmSound = new SoundPathSpecifier("/Audio/_Impstation/Machines/door_alarm.ogg");

    /// <summary>
    /// Alarm sound volume
    /// </summary>
    [DataField]
    public float volume = 8;
    /// <summary>
    /// Alarm sound range
    /// </summary>
    [DataField]
    public float range = 15;


}
