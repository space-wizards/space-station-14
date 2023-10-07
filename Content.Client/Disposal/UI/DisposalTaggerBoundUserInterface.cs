using Content.Shared.Disposal;
using Robust.Client.GameObjects;

namespace Content.Client.Disposal.UI;

/// <summary>
/// Initializes a <see cref="DisposalTaggerWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class DisposalTaggerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DisposalTaggerWindow? _window;

    public DisposalTaggerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new DisposalTaggerWindow();

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.Confirm.OnPressed += _ => ButtonPressed(_window.TagInput.Text);
        _window.TagInput.OnTextEntered += args => ButtonPressed(args.Text);
    }

    private void ButtonPressed(string tag)
    {
        SendMessage(new TaggerSetTagMessage(tag));
        _window?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DisposalTaggerUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
