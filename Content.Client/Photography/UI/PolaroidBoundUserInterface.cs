using Content.Shared.Photography;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Photography.UI;

public sealed class PolaroidBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PalaroidWindow? _window;

    public PolaroidBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new PalaroidWindow();

        _window.OnSaved += text =>
        {
            SendMessage(new PaperInputTextMessage(text));
        };

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not PolaroidBoundUserInterfaceState castState)
            return;

        _window.SetPhoto(castState.RawData, castState.FontSize);

        var paperState = new PaperBoundUserInterfaceState(
            castState.CaptionText,
            castState.StampedBy,
            castState.Mode
        );

        _window.Populate(paperState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
