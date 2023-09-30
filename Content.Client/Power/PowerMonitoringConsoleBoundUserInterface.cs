using Content.Shared.Power;

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

        _menu = new PowerMonitoringWindow(this, gridUid);

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
        _menu?.ShowEntites
            (castState.TotalSources,
            castState.TotalLoads,
            castState.Sources,
            castState.Loads,
            castState.PowerCableChunks,
            xform?.Coordinates,
            castState.Snap,
            castState.Precision);
    }

    public void RequestPowerMonitoringData()
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
