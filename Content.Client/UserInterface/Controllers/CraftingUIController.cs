using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class CraftingUIController : UIController, IOnStateChanged<GameplayState>
{

    private ConstructionMenuPresenter? _presenter;
    private MenuButton CraftingButton => UIManager.GetActiveUIWidget<MenuBar>().CraftingButton;

    public void OnStateChanged(GameplayState state)
    {
        _presenter = new ConstructionMenuPresenter();
        // TODO unsubscribe
        CraftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }
}
