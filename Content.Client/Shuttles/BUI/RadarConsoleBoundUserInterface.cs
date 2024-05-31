using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class RadarConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RadarConsoleWindow? _window;

    public RadarConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new RadarConsoleWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
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
        if (state is not NavBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(cState.State);
    }
}
