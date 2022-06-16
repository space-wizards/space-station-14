using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private ShuttleConsoleWindow? _window;

    public ShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new ShuttleConsoleWindow();
        _window.ShuttleModePressed += OnShuttleModePressed;
        _window.UndockPressed += OnUndockPressed;
        _window.StartAutodockPressed += OnAutodockPressed;
        _window.StopAutodockPressed += OnStopAutodockPressed;
        _window.OpenCentered();
        _window.OnClose += OnClose;
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
        SendMessage(new StopAutodockRequestMessage() {Entity = obj});
    }

    private void OnAutodockPressed(EntityUid obj)
    {
        SendMessage(new AutodockRequestMessage() {Entity = obj});
    }

    private void OnUndockPressed(EntityUid obj)
    {
        SendMessage(new UndockRequestMessage() {Entity = obj});
    }

    private void OnShuttleModePressed(ShuttleMode obj)
    {
        SendMessage(new ShuttleModeRequestMessage() {Mode = obj});
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ShuttleConsoleBoundInterfaceState cState) return;
        _window?.UpdateState(cState);
    }
}
