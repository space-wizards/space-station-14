using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Client.Power.Substation;

[UsedImplicitly]
public sealed class PowerMonitoringSubstationBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PowerMonitoringSubstationWindow? _window;

    public PowerMonitoringSubstationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    public void ButtonPressed()
    {
        SendMessage(new PowerMonitoringUIChangedMessage());
    }

    protected override void Open()
    {
        base.Open();

        _window = new PowerMonitoringSubstationWindow(this);
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
