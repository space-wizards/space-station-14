// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.PipePainter.UI;
using Content.Shared.SS220.PipePainter;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.PipePainter;

public sealed class PipePainterBoundUserInterface : BoundUserInterface
{
    private PipePainterWindow? _window;

    public PipePainterBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PipePainterWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnColorPicked = OnColorPicked;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not PipePainterBoundUserInterfaceState stateCast)
            return;

        _window.Populate(stateCast.Palette, stateCast.SelectedColorKey);
    }

    private void OnColorPicked(string paletteKey)
    {
        SendMessage(new PipePainterSpritePickedMessage(paletteKey));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Dispose();
    }
}
