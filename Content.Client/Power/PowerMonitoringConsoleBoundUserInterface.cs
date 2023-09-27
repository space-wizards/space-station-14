using Content.Shared.Power;
using static Content.Shared.Access.Components.AccessOverriderComponent;

namespace Content.Client.Power;

public sealed class PowerMonitoringConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PowerMonitoringWindow? _menu;

    public PowerMonitoringConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        EntityUid? gridUid = null;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _menu = new PowerMonitoringWindow(gridUid);

        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (PowerMonitoringConsoleBoundInterfaceState) state;

        if (castState == null)
            return;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.ShowEntites(castState.Loads, xform?.Coordinates, castState.Snap, castState.Precision);
    }

    public void RequestData()
    {
        SendMessage(new RequestPowerMonitoringDataMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
