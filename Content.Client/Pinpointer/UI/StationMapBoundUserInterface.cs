using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Pinpointer.UI;

public sealed class StationMapBoundUserInterface : BoundUserInterface
{
    private StationMapWindow? _window;

    public StationMapBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window?.Close();
        EntityUid? gridUid = null;

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent<TransformComponent>(Owner.Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = new StationMapWindow(gridUid, Owner.Owner);
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
    }
}
