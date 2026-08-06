using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.Tabletop.UI;

/// <summary>
/// A bound UI for tabletop games.
/// Sets up the window into the game and handles rotation and drag events.
/// </summary>
public sealed partial class TabletopGameBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TabletopWindow? _window;

    /// <inheritdoc cref="TabletopGameBoundUserInterface"/>
    public TabletopGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TabletopWindow>();

        if (!EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
            return;

        if (EntMan.TryGetComponent<EyeComponent>(tabletop.Board, out var eye))
            _window.SetPosition(eye.Eye, tabletop.Size);

        _window.DragStarted += OnDragStarted;
        _window.DragMoved += OnDragMoved;
        _window.DragFinished += OnDragFinished;
        _window.SetBoard(tabletop.Board);
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
