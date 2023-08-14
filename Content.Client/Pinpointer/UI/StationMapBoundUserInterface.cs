using Robust.Client.GameObjects;

namespace Content.Client.Pinpointer.UI;

public sealed class StationMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationMapWindow? _window;

    public StationMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window?.Close();
        EntityUid? gridUid = null;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = new StationMapWindow(gridUid, Owner);
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
    }
}
