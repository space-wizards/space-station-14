using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Crafting;

[UsedImplicitly]
public sealed class CraftingUIController : UIController, IOnStateChanged<GameplayState>
{
    private ConstructionMenuPresenter? _presenter;
    private MenuButton? _craftingButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_presenter == null);
        _presenter = new ConstructionMenuPresenter();
        _craftingButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.GameTopMenuBar>().CraftingButton;
        _craftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }

    public void OnStateExited(GameplayState state)
    {
        if (_presenter == null)
            return;
        _craftingButton!.Pressed = false;
        _craftingButton!.OnToggled -= _presenter.OnHudCraftingButtonToggled;
        _craftingButton = null;
        _presenter.Dispose();
        _presenter = null;
    }
}
