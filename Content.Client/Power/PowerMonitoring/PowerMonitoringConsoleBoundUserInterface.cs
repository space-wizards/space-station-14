using Content.Client.Computer;
using Content.Client.Power.PowerMonitoring;
using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.PowerMonitoring;

[UsedImplicitly]
public sealed class PowerMonitoringConsoleBoundUserInterface : ComputerBoundUserInterface<PowerMonitoringConsoleWindow, PowerMonitoringBoundInterfaceState>
{
    public PowerMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }
}
