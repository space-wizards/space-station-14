using Content.Shared.Changeling;
using Robust.Client.GameObjects;

namespace Content.Client.Changeling.Ui;

public sealed class TransformationsBoundUserInterface : BoundUserInterface
{
    private TransformationsMenu? _window;

    public TransformationsBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new TransformationsMenu();
        _window.OnClose += Close;
        _window.OnTransformAction += Transform;
        _window.OpenCentered();
    }

    private void Transform(string name)
    {
        SendMessage(new ChangelingTransformMessage(name));
        Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not TransformationsBoundUserInterfaceState cast)
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
