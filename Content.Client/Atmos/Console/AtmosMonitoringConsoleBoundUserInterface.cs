using Content.Shared.Atmos.Components;

namespace Content.Client.Atmos.Console;

public sealed class AtmosMonitoringConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AtmosMonitoringConsoleWindow? _menu;

    public AtmosMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _menu = new AtmosMonitoringConsoleWindow(this, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (AtmosMonitoringConsoleBoundInterfaceState)state;

        if (castState == null)
            return;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.UpdateUI(xform?.Coordinates, castState.Pumps, castState.Mixers, castState.Filters, castState.Thermoregulators, castState.FocusData);
    }

    public void SendFocusChangeMessage(NetEntity? netEntity)
    {
        SendMessage(new AtmosMonitoringConsoleFocusChangeMessage(netEntity));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
