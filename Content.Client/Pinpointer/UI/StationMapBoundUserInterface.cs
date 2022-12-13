using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Pinpointer.UI;

public sealed class StationMapBoundUserInterface : BoundUserInterface
{
    private DummyNavMapWindow? _window = null;

    public StationMapBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window?.Close();
        var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
        EntityUid? gridUid = null;

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent<TransformComponent>(player, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = new DummyNavMapWindow(gridUid);
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
    }
}
