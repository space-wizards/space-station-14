using Robust.Client.GameObjects;

namespace Content.Client.Fax.UI;

public sealed class FaxBoundUi : BoundUserInterface
{
    private FaxWindow? _window;
    
    public FaxBoundUi(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new FaxWindow();
        _window.OpenCentered();

        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
