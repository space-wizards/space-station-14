using Content.Shared.Atmos.Monitor;
using Robust.Shared.Audio;

namespace Content.Server.Atmos.Monitor.Components
{
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
    // -> AtmosMonitorAlarmEvent
    // -> Everything linked to that monitor (targetted)

    /// <summary>
    ///     A component to add to device network devices if you want them to be alarmed
    ///     by an atmospheric alarmer. This will store every single alert received, and
    ///     calculate the highest alert based on the alerts received. Equally, if you
    ///     link other alarmables to this, it will store the alerts from them to
    ///     calculate the highest network alert.
    /// </summary>
    [RegisterComponent]
    public sealed class AtmosAlarmableComponent : Component
    {
        [ViewVariables]
        public readonly Dictionary<string, AtmosMonitorAlarmType> NetworkAlarmStates = new();

        [ViewVariables] public AtmosMonitorAlarmType LastAlarmState = AtmosMonitorAlarmType.Normal;

        [ViewVariables] public bool IgnoreAlarms { get; set; } = false;

        [DataField("alarmSound")]
        public SoundSpecifier AlarmSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

        [DataField("alarmVolume")]
        public float AlarmVolume { get; set; } = -10;

        /// <summary>
        ///     List of prototypes that this alarmable can
        ///     sync with - this is so that you can sync without
        ///     having to worry about cross-contamination.
        /// </summary>
        [DataField("syncWith")]
        public List<string> SyncWithPrototypes { get; } = new();

        /// <summary>
        ///     If this device should receive only. If it can only
        ///     receive, that means that attempting to sync outwards
        ///     will result in nothing happening.
        /// </summary>
        [DataField("receiveOnly")]
        public bool ReceiveOnly { get; }
    }
}
