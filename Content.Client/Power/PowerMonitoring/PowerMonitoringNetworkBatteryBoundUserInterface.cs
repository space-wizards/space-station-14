using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.PowerMonitoring;

[UsedImplicitly]
public sealed class PowerMonitoringNetworkBatteryBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PowerMonitoringNetworkBatteryWindow? _window;

    public PowerMonitoringNetworkBatteryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _window = new PowerMonitoringNetworkBatteryWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (PowerMonitoringBoundInterfaceState) state;
        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
