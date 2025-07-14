using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

[UsedImplicitly]
public sealed class GasTankBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private GasTankWindow? _window;

    public void Update(GasTankComponent tankComp)
    {
        _window?.UpdateDisplay(tankComp.OutputPressure,
            tankComp.Air.Pressure,
            tankComp.IsValveOpen,
            tankComp.IsConnected);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<GasTankWindow>();
        _window.ToggleInternalsPressed += OnToggleInternals;
        _window.OnSetPressure += OnSetPressure;
        _window.OnToggleValvePressed += OnPressValve;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is GasTankUpdateMessage update)
        {
            _window.UpdateDisplay(update.Pressure, update.AirPressure, update.GasValve, update.InternalsConnected);
        }
    }

    private void OnToggleInternals()
    {
        SendMessage(new GasTankToggleInternalsMessage());
    }

    private void OnSetPressure(float pressure)
    {
        SendMessage(new GasTankSetPressureMessage(pressure));
    }

    private void OnPressValve()
    {
        SendMessage(new GasTankToggleValveMessage());
    }
}
