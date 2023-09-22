using Content.Client.Power.PowerMonitoring;
using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.SMES;

[UsedImplicitly]
public sealed class PowerMonitoringSMESBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PowerMonitoringSMESWindow? _window;

    public PowerMonitoringSMESBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _window = new PowerMonitoringSMESWindow(this);
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
