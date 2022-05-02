using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Widgets;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed class CraftingUIController : UIController, IOnStateEntered<GameplayState>
{

    private ConstructionMenuPresenter? _presenter;
    private MenuButton CraftingButton => UIManager.GetActiveUIWidget<MenuBar>().CraftingButton;

    public void OnStateEntered(GameplayState state)
    {
        _presenter = new ConstructionMenuPresenter();
        // TODO unsubscribe
        CraftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }
}
