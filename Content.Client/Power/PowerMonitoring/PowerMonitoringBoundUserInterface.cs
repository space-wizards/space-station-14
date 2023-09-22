using Content.Client.Computer;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Power.PowerMonitoring;

[UsedImplicitly]
public sealed class PowerMonitoringBoundUserInterface : ComputerBoundUserInterface<PowerMonitoringWindow, PowerMonitoringBoundInterfaceState>
{
    public PowerMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    public void ButtonPressed()
    {
        SendMessage(new PowerMonitoringUIChangedMessage());
    }
}
