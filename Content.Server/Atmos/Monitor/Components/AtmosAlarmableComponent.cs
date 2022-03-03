using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
    ///     by an atmospheric monitor.
    /// </summary>
    [RegisterComponent]
    public sealed class AtmosAlarmableComponent : Component
    {
        [ViewVariables]
        public List<EntityUid> LinkedMonitors { get; set; } = new();

        [ViewVariables] public AtmosMonitorAlarmType LastAlarmState = AtmosMonitorAlarmType.Normal;
        [ViewVariables] public AtmosMonitorAlarmType HighestNetworkState = AtmosMonitorAlarmType.Normal;
        [ViewVariables] public bool IgnoreAlarms { get; set; } = false;

        /// <summary>
        ///     List of prototypes that this alarmable can be
        ///     alarmed by - must be a prototype with AtmosMonitor
        ///     attached to it
        /// </summary>
        [DataField("alarmedBy")]
        public List<string> AlarmedByPrototypes { get; } = new();
    }
}
