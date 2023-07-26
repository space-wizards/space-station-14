using Content.Client.PipePainter.UI;
using Content.Shared.PipePainter;
using Robust.Client.GameObjects;

namespace Content.Client.PipePainter;

public sealed class PipePainterBoundUserInterface : BoundUserInterface
{
    private PipePainterWindow? _window;

    public PipePainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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
