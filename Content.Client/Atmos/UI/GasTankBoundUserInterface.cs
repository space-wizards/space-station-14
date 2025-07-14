using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

[UsedImplicitly]
public sealed class GasTankBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private GasTank? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<GasTank>();
        _window.ToggleInternalsPressed += OnToggleInternals;
        _window.OnSetPressure += OnSetPressure;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is GasTankUpdateMessage update)
        {
            _window.UpdateDisplay(update.Pressure, update.InternalsConnected);
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
}
