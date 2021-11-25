using System.Collections.Generic;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.CrewMonitoring
{
    [RegisterComponent]
    [Friend(typeof(CrewMonitoringConsoleSystem))]
    public class CrewMonitoringConsoleComponent : Component
    {
        public override string Name => "CrewMonitoringConsole";

        public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();
    }
}
