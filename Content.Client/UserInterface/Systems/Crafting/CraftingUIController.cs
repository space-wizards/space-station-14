using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Crafting;

public sealed class CraftingUIController : UIController, IOnStateEntered<GameplayState>
{

    private ConstructionMenuPresenter? _presenter;
    private MenuButton CraftingButton => UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().CraftingButton;

    public void OnStateEntered(GameplayState state)
    {
        _presenter = new ConstructionMenuPresenter();
        // TODO unsubscribe
        CraftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }
}
