using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.HUD.Widgets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed class CraftingUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IHudManager _hud = default!;

    private ConstructionMenuPresenter? _presenter;
    private MenuButton CraftingButton => _hud.GetUIWidget<MenuBar>().CraftingButton;

    public void OnStateChanged(GameplayState state)
    {
        _presenter = new ConstructionMenuPresenter();
        // TODO unsubscribe
        CraftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }
}
