using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Messages;
using Content.Shared.Arcade.Messages.KudzuCrush;
using Content.Shared.IdentityManagement;
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
        _window.Title = Identity.Name(Owner, EntMan);
        _window.NewGameButton.OnPressed += _ => SendPredictedMessage(new ArcadeNewGameMessage());

        _window.OnAction += action =>
        {
            switch (action)
            {
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
        };

        if (EntMan.TryGetComponent<ArcadeComponent>(Owner, out var arcade) && arcade.Player != _playerManager.LocalEntity)
            _window.SetUsability(false);

        Update();

        _window.OpenCentered();
    }

    public override void Update()
    {
        base.Update();

        if (_window is null || !EntMan.TryGetComponent(Owner, out KudzuCrushArcadeComponent? comp))
            return;

        _window.UpdateGrid(comp.GridSize.X, comp.Grid);
    }
}
