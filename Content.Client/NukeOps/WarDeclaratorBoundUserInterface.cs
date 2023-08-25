using Content.Shared.NukeOps;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.NukeOps;

[UsedImplicitly]
public sealed class WarDeclaratorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private WarDeclaratorWindow? _window;

    public WarDeclaratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = new WarDeclaratorWindow();
        if (State != null)
            UpdateState(State);

        _window.OpenCentered();

        _window.OnClose += Close;
        _window.OnActivated += OnWarDeclaratorActivated;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not WarDeclaratorBoundUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _window?.Dispose();
    }

    private void OnWarDeclaratorActivated(string message)
    {
        SendMessage(new WarDeclaratorActivateMessage(message));
    }
}
