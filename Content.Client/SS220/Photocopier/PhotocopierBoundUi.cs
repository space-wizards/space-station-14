// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Content.Client.SS220.Photocopier.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Photocopier;

/// <inheritdoc />
public sealed class PhotocopierBoundUi : BoundUserInterface
{
    private PhotocopierWindow? _window;

    public PhotocopierBoundUi(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotocopierWindow();
        _window.OpenCentered();

        _window.OnClose += Close;
        _window.PrintButtonPressed += OnPrintButtonPressed;
        _window.CopyButtonPressed += OnCopyButtonPressed;
        _window.EjectButtonPressed += OnEjectButtonPressed;
        _window.StopButtonPressed += OnStopButtonPressed;
        _window.RefreshButtonPressed += OnRefreshButtonPressed;
    }

    private void OnRefreshButtonPressed()
    {
        SendMessage(new PhotocopierRefreshUiMessage());
    }

    private void OnPrintButtonPressed(int amount, FormDescriptor descriptor)
    {
        SendMessage(new PhotocopierPrintMessage(amount, descriptor));
    }

    private void OnCopyButtonPressed(int amount)
    {
        SendMessage(new PhotocopierCopyMessage(amount));
    }

    private void OnEjectButtonPressed()
    {
        SendMessage(new ItemSlotButtonPressedEvent(PhotocopierComponent.PaperSlotId, true, false));
    }

    private void OnStopButtonPressed()
    {
        SendMessage(new PhotocopierStopMessage());
    }

    /// <inheritdoc />
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window is null || state is not PhotocopierUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing)
            _window?.Dispose();
    }
}
