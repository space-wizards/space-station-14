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

    }
}
