using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

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
        _window.UndockPressed += OnUndockPressed;
        _window.StartAutodockPressed += OnAutodockPressed;
        _window.StopAutodockPressed += OnStopAutodockPressed;
        _window.DestinationPressed += OnDestinationPressed;
        _window.OpenCentered();
        _window.OnClose += OnClose;
    }

    private void OnDestinationPressed(EntityUid obj)
    {
        SendMessage(new ShuttleConsoleFTLRequestMessage()
        {
            Destination = EntMan.ToNetEntity(obj),
        });
    }

    private void OnClose()
    {
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    private void OnStopAutodockPressed(EntityUid obj)
    {
        SendMessage(new StopAutodockRequestMessage() { DockEntity = EntMan.ToNetEntity(obj) });
    }

    private void OnAutodockPressed(EntityUid obj)
    {
        SendMessage(new AutodockRequestMessage() { DockEntity = EntMan.ToNetEntity(obj) });
    }

    private void OnUndockPressed(EntityUid obj)
    {
        SendMessage(new UndockRequestMessage() { DockEntity = EntMan.ToNetEntity(obj) });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ShuttleConsoleBoundInterfaceState cState) return;

        _window?.SetMatrix(EntMan.ToCoordinates(cState.Coordinates), cState.Angle);
        _window?.UpdateState(cState);
    }
}
