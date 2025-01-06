using Content.Shared._EinsteinEngines.Supermatter.Components;

namespace Content.Client._EinsteinEngines.Supermatter.Consoles;

public sealed class SupermatterConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SupermatterConsoleWindow? _menu;

    public SupermatterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _menu = new SupermatterConsoleWindow(this, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (SupermatterConsoleBoundInterfaceState)state;
        _menu?.UpdateUI(castState.Supermatters, castState.FocusData);
    }

    public void SendFocusChangeMessage(NetEntity? netEntity)
    {
        SendMessage(new SupermatterConsoleFocusChangeMessage(netEntity));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
