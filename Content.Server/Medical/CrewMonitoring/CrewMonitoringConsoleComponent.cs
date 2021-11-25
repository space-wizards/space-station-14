using System.Collections.Generic;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Medical.CrewMonitoring
{
    [RegisterComponent]
    [Friend(typeof(CrewMonitoringConsoleSystem))]
    public class CrewMonitoringConsoleComponent : Component
    {
        public override string Name => "CrewMonitoringConsole";

        /// <summary>
        ///     List of all currently connected sensors to this console.
        /// </summary>
        public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

        /// <summary>
        ///     After what time sensor consider to be lost.
        /// </summary>
        [DataField("sensorTimeout")]
        public float SensorTimeout = 10f;
    }
}
