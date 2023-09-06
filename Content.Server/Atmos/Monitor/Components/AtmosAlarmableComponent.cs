using Content.Shared.Atmos.Monitor;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Atmos.Monitor.Components;
// AtmosAlarmables are entities that can be alarmed
// by a linked AtmosMonitor (alarmer?) if a threshold
// is passed in some way. The intended use is to
// do something in case something dangerous happens,
// e.g., activate firelocks in case a temperature
// threshold is reached
//
// It goes:
//
// AtmosMonitor -> AtmosDeviceUpdateEvent
// -> Threshold calculation
// -> AtmosAlarmEvent
// -> Everything linked to that monitor (targetted)

/// <summary>
///     A component to add to device network devices if you want them to be alarmed
///     by an atmospheric alarmer. This will store every single alert received, and
///     calculate the highest alert based on the alerts received. Equally, if you
///     link other alarmables to this, it will store the alerts from them to
///     calculate the highest network alert.
/// </summary>
[RegisterComponent]
public sealed partial class AtmosAlarmableComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<string, AtmosAlarmType> NetworkAlarmStates = new();

    [ViewVariables] public AtmosAlarmType LastAlarmState = AtmosAlarmType.Invalid;

    [ViewVariables] public bool IgnoreAlarms { get; set; } = false;

    [DataField("alarmSound")]
    public SoundSpecifier AlarmSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

    [DataField("alarmVolume")]
    public float AlarmVolume { get; set; } = -10;

    /// <summary>
    ///     List of tags to check for when synchronizing alarms.
    /// </summary>
    [DataField("syncWith", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TagPrototype>))]
    public HashSet<string> SyncWithTags { get; private set; } = new();

    [DataField("monitorAlertTypes")]
    public HashSet<AtmosMonitorThresholdType>? MonitorAlertTypes { get; private set; }

    /// <summary>
    ///     If this device should receive only. If it can only
    ///     receive, that means that attempting to sync outwards
    ///     will result in nothing happening.
    /// </summary>
    [DataField("receiveOnly")]
    public bool ReceiveOnly { get; private set; }
}
