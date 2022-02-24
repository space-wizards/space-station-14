using System.Collections.Generic;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public sealed class AtmosMonitorComponent : Component
    {
        // Whether this monitor can send alarms,
        // or recieve atmos command events.
        //
        // Useful for wires; i.e., pulsing a monitor wire
        // will make it send an alert, and cutting
        // it will make it so that alerts are no longer
        // sent/receieved.
        //
        // Note that this cancels every single network
        // event, including ones that may not be
        // related to atmos monitor events.
        [ViewVariables]
        public bool NetEnabled = true;

        // Entities that the monitor will alarm. Stores only EntityUids, is populated
        // when this component starts up.
        [ViewVariables]
        public List<EntityUid> LinkedEntities = new();

        [DataField("temperatureThreshold", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThreshold>)))]
        public readonly string? TemperatureThresholdId;

        [ViewVariables]
        public AtmosAlarmThreshold? TemperatureThreshold;

        [DataField("pressureThreshold", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThreshold>)))]
        public readonly string? PressureThresholdId;

        [ViewVariables]
        public AtmosAlarmThreshold? PressureThreshold;

        // monitor fire - much different from temperature
        // since there's events for fire, setting this to true
        // will make the atmos monitor act like a smoke detector,
        // immediately signalling danger if there's a fire
        [DataField("monitorFire")]
        public bool MonitorFire = false;

        [DataField("displayMaxAlarmInNet")]
        public bool DisplayMaxAlarmInNet = false;

        [DataField("alarmSound")]
        public SoundSpecifier AlarmSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

        [DataField("alarmVolume")]
        public float AlarmVolume { get; set; } = -10;

        // really messy but this is parsed at runtime after
        // prototypes are initialized, there's no
        // way without implementing a new
        // type serializer
        [DataField("gasThresholds")]
        public Dictionary<Gas, string>? GasThresholdIds;

        [ViewVariables]
        public Dictionary<Gas, AtmosAlarmThreshold>? GasThresholds;

        // Stores a reference to the gas on the tile this is on.
        [ViewVariables]
        public GasMixture? TileGas;

        // Stores the last alarm state of this alarm.
        [ViewVariables]
        public AtmosMonitorAlarmType LastAlarmState = AtmosMonitorAlarmType.Normal;

        // feeling real dirty about this one
        // Caches the alarm states it recieves from the rest of the network.
        // This is so that the highest alarm in the network can be calculated
        // from any monitor without having to reping every alarm.
        [ViewVariables]
        public Dictionary<string, AtmosMonitorAlarmType> NetworkAlarmStates = new();

        // Calculates the highest alarm in the network, including itself.
        [ViewVariables]
        public AtmosMonitorAlarmType HighestAlarmInNetwork
        {
            get
            {
                var state = AtmosMonitorAlarmType.Normal;
                foreach (var (_, netState) in NetworkAlarmStates)
                    if (state < netState)
                        state = netState;

                if (LastAlarmState > state) state = LastAlarmState;

                return state;
            }
        }
    }
}
