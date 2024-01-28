using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ShuttleConsoleWindow? _window;

    public ShuttleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new ShuttleConsoleWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ShuttleConsoleBoundInterfaceState cState)
            return;

        _window?.SetMatrix(EntMan.GetCoordinates(cState.Coordinates), cState.Angle);
        _window?.UpdateState(cState);
    }
}
