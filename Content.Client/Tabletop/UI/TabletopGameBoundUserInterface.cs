using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.Tabletop.UI;

/// <summary>
/// A bound UI for tabletop games.
/// Sets up the viewer eye and handles rotation, and that's about it!
/// </summary>
public sealed class TabletopGameBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TabletopWindow? _window;

    [ViewVariables]
    private bool _upright;

    public TabletopGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TabletopWindow>();

        if (!EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
            return;

        if (EntMan.TryGetComponent<EyeComponent>(tabletop.UprightCamera, out var eye))
            _window.SetPosition(eye.Eye, tabletop.Size);

        _window.FlipPressed += FlipCamera;
        _window.DragStarted += OnDragStarted;
        _window.DragMoved += OnDragMoved;
        _window.DragFinished += OnDragFinished;
        _window.SetBoard(tabletop.Board);
        _upright = true;
    }

    private void FlipCamera()
    {
        if (_window is null)
            return;

        if (!EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
            return;

        var targetCamera = _upright ? tabletop.UpsideDownCamera : tabletop.UprightCamera;
        if (!EntMan.TryGetComponent<EyeComponent>(targetCamera, out var eye))
            return;

        _window.SetPosition(eye.Eye, tabletop.Size);
        _upright = !_upright;
    }

    private void OnDragStarted(EntityUid piece)
    {
        var netPiece = EntMan.GetNetEntity(piece);
        EntMan.RaisePredictiveEvent(new TabletopDraggingPlayerChangedEvent(netPiece, true));
    }

    private void OnDragMoved(EntityUid piece, EntityCoordinates coordinates)
    {
        var netPiece = EntMan.GetNetEntity(piece);
        var netTable = EntMan.GetNetEntity(Owner);
        EntMan.RaisePredictiveEvent(new TabletopMoveEvent(netPiece, coordinates.Position, netTable));
    }

    private void OnDragFinished(EntityUid piece)
    {
        var netPiece = EntMan.GetNetEntity(piece);
        EntMan.RaisePredictiveEvent(new TabletopDraggingPlayerChangedEvent(netPiece, false));
    }
}
