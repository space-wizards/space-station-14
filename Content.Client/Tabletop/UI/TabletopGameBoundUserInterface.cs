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

    [ViewVariables]
    private EntityUid? _lastBoard;

    /// <inheritdoc cref="TabletopGameBoundUserInterface"/>
    public TabletopGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();
        IoCManager.InjectDependencies(this);

        _window = this.CreateWindow<TabletopWindow>();

        _window.DragStarted += OnDragStarted;
        _window.DragMoved += OnDragMoved;
        _window.DragFinished += OnDragFinished;

        if (!EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
            return;

        UpdateBoardState((Owner, tabletop));
    }

    /// <inheritdoc />
    public override void Update()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
            return;

        if (_lastBoard == tabletop.Board)
            return;

        UpdateBoardState((Owner, tabletop));
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

    /// <summary>
    /// Sets the board up in case it has changed.
    /// </summary>
    private void UpdateBoardState(Entity<TabletopGameComponent> tabletop)
    {
        if (_window == null)
            return;

        _lastBoard = tabletop.Comp.Board;

        if (EntMan.TryGetComponent<EyeComponent>(tabletop.Comp.Board, out var eye))
            _window.SetPosition(eye.Eye, tabletop.Comp.Size);

        _window.SetBoard(tabletop.Comp.Board);
    }
}
