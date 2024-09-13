using Content.Shared.Power;
using Robust.Client.UserInterface;

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

        _window = this.CreateWindow<PowerChargeWindow>();
        _window.UpdateWindow(this, Loc.GetString(component.WindowTitle));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not PowerChargeState chargeState)
            return;

        _window?.UpdateState(chargeState);
    }
}
