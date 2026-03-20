using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Messages;
using Content.Shared.Arcade.Messages.KudzuCrush;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

/// <summary>
///
/// </summary>
public sealed class KudzuCrushArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private KudzuCrushArcadeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<KudzuCrushArcadeWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.NewGameButton.OnPressed += _ => SendPredictedMessage(new ArcadeNewGameMessage());

        _window.OnAction += OnAction;

        if (EntMan.TryGetComponent<KudzuCrushArcadeComponent>(Owner, out var kudzuCrush))
            _window.CreateGrid(kudzuCrush.GridSize.X, kudzuCrush.Grid);

        if (EntMan.TryGetComponent<ArcadeComponent>(Owner, out var arcade) && arcade.Player != _playerManager.LocalEntity)
            _window.SetUsability(false);

        _window.OpenCentered();
    }

    private void OnAction(KudzuCrushArcadeAction action)
    {
        switch (action)
        {
            case KudzuCrushArcadeAction.Down:
                SendPredictedMessage(new KudzuCrushArcadeActionDownMessage());
                break;
            case KudzuCrushArcadeAction.Left:
                SendPredictedMessage(new KudzuCrushArcadeActionLeftMessage());
                break;
            case KudzuCrushArcadeAction.Right:
                SendPredictedMessage(new KudzuCrushArcadeActionRightMessage());
                break;
            case KudzuCrushArcadeAction.Drop:
                SendPredictedMessage(new KudzuCrushArcadeActionDropMessage());
                break;
            case KudzuCrushArcadeAction.Rotate:
                SendPredictedMessage(new KudzuCrushArcadeActionRotateMessage());
                break;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public void CreateGrid(int gridWidth, KudzuCrushArcadeCell[] grid)
    {
        _window?.CreateGrid(gridWidth, grid);
    }

    /// <summary>
    ///
    /// </summary>
    public void UpdateGridCell(int index, KudzuCrushArcadeCell cell)
    {
        _window?.UpdateGridCell(index, cell);
    }
}
