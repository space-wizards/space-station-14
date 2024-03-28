using Content.Shared.Power;

namespace Content.Client.Power.PowerCharge;

public sealed class PowerChargeBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PowerChargeWindow? _window;

    public PowerChargeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    public void SetPowerSwitch(bool on)
    {
        SendMessage(new SwitchChargingMachineMessage(on));
    }

    protected override void Open()
    {
        base.Open();
        if (!EntMan.TryGetComponent(Owner, out PowerChargeComponent? component))
            return;

        _window = new PowerChargeWindow(this, component.WindowTitle);
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not PowerChargeState chargeState)
            return;

        _window?.UpdateState(chargeState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}
