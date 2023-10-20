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

    private void OnDestinationPressed(NetEntity obj)
    {
        SendMessage(new ShuttleConsoleFTLRequestMessage()
        {
            Destination = obj,
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

    private void OnStopAutodockPressed(NetEntity obj)
    {
        SendMessage(new StopAutodockRequestMessage() { DockEntity = obj });
    }

    private void OnAutodockPressed(NetEntity obj)
    {
        SendMessage(new AutodockRequestMessage() { DockEntity = obj });
    }

    private void OnUndockPressed(NetEntity obj)
    {
        SendMessage(new UndockRequestMessage() { DockEntity = obj });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ShuttleConsoleBoundInterfaceState cState) return;

        _window?.SetMatrix(EntMan.GetCoordinates(cState.Coordinates), cState.Angle);
        _window?.UpdateState(cState);
    }
}
