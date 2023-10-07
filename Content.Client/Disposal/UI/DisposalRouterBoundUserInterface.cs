using Content.Shared.Disposal;
using Robust.Client.GameObjects;

namespace Content.Client.Disposal.UI;

/// <summary>
/// Initializes a <see cref="DisposalRouterWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class DisposalRouterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DisposalRouterWindow? _window;

    public DisposalRouterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new DisposalRouterWindow();

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.Confirm.OnPressed += _ => ButtonPressed(_window.TagsInput.Text);
        _window.TagsInput.OnTextEntered += args => ButtonPressed(args.Text);
    }

    private void ButtonPressed(string tags)
    {
        SendMessage(new RouterSetTagsMessage(tags));
        _window?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DisposalRouterUserInterfaceState cast)
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
