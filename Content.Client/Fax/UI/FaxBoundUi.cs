using Content.Shared.Fax;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Fax.UI;

[UsedImplicitly]
public sealed class FaxBoundUi : BoundUserInterface
{
    [ViewVariables]
    private FaxWindow? _window;

    public FaxBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new FaxWindow();
        _window.OpenCentered();

        _window.OnClose += Close;
        _window.CopyButtonPressed += OnCopyButtonPressed;
        _window.SendButtonPressed += OnSendButtonPressed;
        _window.RefreshButtonPressed += OnRefreshButtonPressed;
        _window.PeerSelected += OnPeerSelected;
    }

    private void OnSendButtonPressed()
    {
        SendMessage(new FaxSendMessage());
    }

    private void OnCopyButtonPressed()
    {
        SendMessage(new FaxCopyMessage());
    }

    private void OnRefreshButtonPressed()
    {
        SendMessage(new FaxRefreshMessage());
    }

    private void OnPeerSelected(string address)
    {
        SendMessage(new FaxDestinationMessage(address));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not FaxUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
